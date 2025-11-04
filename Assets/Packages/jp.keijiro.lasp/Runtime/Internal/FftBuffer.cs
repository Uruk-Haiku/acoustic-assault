using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lasp
{
    //
    // Burst-optimized FFT with YinFFT pitch detection
    //
    sealed class FftBuffer : System.IDisposable
    {
        #region YinFFT Constants

        // Frequency bins for spectral weighting interpolation
        static readonly float[] FreqsMask = {
            0f, 20f, 25f, 31.5f, 40f, 50f, 63f, 80f,
            100f, 125f, 160f, 200f, 250f, 315f, 400f, 500f,
            630f, 800f, 1000f, 1250f, 1600f, 2000f, 2500f, 3150f,
            4000f, 5000f, 6300f, 8000f, 9000f, 10000f, 12500f, 15000f,
            20000f, 25100f
        };

        // YinFFT custom weights (in dB) - optimized for pitch detection
        static readonly float[] YinFFTWeights = {
            -75.8f, -70.1f, -60.8f, -52.1f, -44.2f, -37.5f,
            -31.3f, -25.6f, -20.9f, -16.5f, -12.6f, -9.6f,
            -7.0f, -4.7f, -3.0f, -1.8f, -0.8f, -0.2f,
            -0.0f, 0.5f, 1.6f, 3.2f, 5.4f, 7.8f,
            8.1f, 5.3f, -2.4f, -11.1f, -12.8f, -12.2f,
            -7.4f, -17.8f, -17.8f, -17.8f
        };

        #endregion

        #region Public properties

        public int Width => _N;
        public NativeArray<float> Spectrum => _O;

        #endregion

        #region IDisposable implementation

        public void Dispose()
        {
            if (_I.IsCreated) _I.Dispose();
            if (_O.IsCreated) _O.Dispose();
            if (_W.IsCreated) _W.Dispose();
            if (_P.IsCreated) _P.Dispose();
            if (_T.IsCreated) _T.Dispose();
            if (_spectralWeights.IsCreated) _spectralWeights.Dispose();
            if (_yin.IsCreated) _yin.Dispose();
        }

        #endregion

        #region Public methods

        public FftBuffer(int width, float sampleRate)
        {
            _N = width;
            _logN = (int)math.log2(width);
            _sampleRate = sampleRate;

            _I = PersistentMemory.New<float>(_N);
            _O = PersistentMemory.New<float>(_N / 2);

            InitializeHanningWindow();
            InitializeSpectralWeights();
            BuildPermutationTable();
            BuildTwiddleFactors();

            // YinFFT specific arrays
            _yin = PersistentMemory.New<float>(_N / 2 + 1);
        }

        // Push audio data to the FIFO buffer.
        public void Push(NativeSlice<float> data)
        {
            var length = data.Length;

            if (length == 0) return;

            if (length < _N)
            {
                // The data is smaller than the buffer: Dequeue and copy
                var part = _N - length;
                NativeArray<float>.Copy(_I, _N - part, _I, 0, part);
                data.CopyTo(_I.GetSubArray(part, length));
            }
            else
            {
                // The data is larger than the buffer: Simple fill
                data.Slice(length - _N).CopyTo(_I);
            }
        }

        // Original spectrum analysis for visualization
        public void Analyze(float floor, float head)
        {
            using (var X = TempJobMemory.New<float4>(_N / 2))
            {
                // Bit-reversal permutation and first DFT pass
                new FirstPassJob { I = _I, W = _W, P = _P, X = X }.Run(_N / 2);

                // 2nd and later DFT passes
                for (var i = 0; i < _logN - 1; i++)
                {
                    var T_slice = new NativeSlice<TFactor>(_T, _N / 4 * i);
                    new DftPassJob { T = T_slice, X = X }.Run(_N / 4);
                }

                // Postprocess (power spectrum calculation in dB)
                var O2 = _O.Reinterpret<float2>(sizeof(float));
                new PostprocessJob
                {
                    X = X,
                    O = O2,
                    DivN = 2.0f / _N,
                    DivR = 1 / (head - floor),
                    F = floor
                }.Run(_N / 4);
            }
        }

        // YinFFT pitch detection - returns pitch in Hz and confidence [0,1]
        public (float pitch, float confidence) DetectPitch(float minFreq, float maxFreq)
        {
            // First, get the spectrum using regular FFT
            using (var X = TempJobMemory.New<float4>(_N / 2))
            {
                // Bit-reversal permutation and first DFT pass with Hanning window
                new FirstPassJob { I = _I, W = _W, P = _P, X = X }.Run(_N / 2);

                // 2nd and later DFT passes
                for (var i = 0; i < _logN - 1; i++)
                {
                    var T_slice = new NativeSlice<TFactor>(_T, _N / 4 * i);
                    new DftPassJob { T = T_slice, X = X }.Run(_N / 4);
                }

                // Build squared magnitude spectrum with spectral weighting
                var sqrMag = TempJobMemory.New<float>(_N);
                try
                {
                    float sum = 0;

                    // FIXED: Properly unpack float4 structure
                    // Each X[i] contains TWO complex values packed as (real1, imag1, real2, imag2)
                    for (int i = 0; i < X.Length; i++)
                    {
                        // First complex number at frequency bin i*2
                        int bin1 = i * 2;
                        if (bin1 < _N / 2)
                        {
                            float mag2_1 = X[i].x * X[i].x + X[i].y * X[i].y;
                            float weightedMag1 = mag2_1 * _spectralWeights[bin1];
                            sqrMag[bin1] = weightedMag1;

                            // Mirror for symmetric spectrum (except DC)
                            if (bin1 > 0)
                            {
                                sqrMag[_N - bin1] = weightedMag1;
                            }
                            sum += weightedMag1;
                        }

                        // Second complex number at frequency bin i*2+1
                        int bin2 = i * 2 + 1;
                        if (bin2 < _N / 2)
                        {
                            float mag2_2 = X[i].z * X[i].z + X[i].w * X[i].w;
                            float weightedMag2 = mag2_2 * _spectralWeights[bin2];
                            sqrMag[bin2] = weightedMag2;

                            // Mirror for symmetric spectrum
                            sqrMag[_N - bin2] = weightedMag2;
                            sum += weightedMag2;
                        }
                    }

                    // Handle Nyquist frequency if present
                    sqrMag[_N / 2] = 0;

                    sum *= 2; // Account for mirrored part

                    if (sum == 0)
                    {
                        return (0f, 0f); // Silent frame
                    }

                    // Now do FFT on the symmetric squared magnitude spectrum
                    // This acts as IFFT and gives us autocorrelation
                    using (var XAuto = TempJobMemory.New<float4>(_N / 2))
                    {
                        var noWindow = TempJobMemory.New<float>(_N);
                        try
                        {
                            for (int i = 0; i < _N; i++) noWindow[i] = 1.0f;

                            // Run FFT on squared magnitudes (acts as IFFT for autocorrelation)
                            new FirstPassJob { I = sqrMag, W = noWindow, P = _P, X = XAuto }.Run(_N / 2);

                            for (var i = 0; i < _logN - 1; i++)
                            {
                                var T_slice = new NativeSlice<TFactor>(_T, _N / 4 * i);
                                new DftPassJob { T = T_slice, X = XAuto }.Run(_N / 4);
                            }

                            // Build Yin difference function
                            // Now we need to unpack XAuto correctly too!
                            float tmp = 0;
                            _yin[0] = 1.0f;

                            int tauMax = math.min((int)math.ceil(_sampleRate / minFreq), _N / 2);
                            int tauMin = math.max(2, (int)math.floor(_sampleRate / maxFreq));

                            for (int tau = 1; tau < _yin.Length; tau++)
                            {
                                // Get the real part of autocorrelation at lag tau
                                float realPart = 0;

                                // Find which float4 contains this tau
                                int float4Index = tau / 2;
                                int componentIndex = tau % 2;

                                if (float4Index < XAuto.Length)
                                {
                                    if (componentIndex == 0)
                                    {
                                        // First complex number in the float4
                                        realPart = XAuto[float4Index].x;
                                    }
                                    else
                                    {
                                        // Second complex number in the float4
                                        realPart = XAuto[float4Index].z;
                                    }
                                }

                                // Yin difference function
                                _yin[tau] = sum - realPart;

                                // Cumulative mean normalization
                                tmp += _yin[tau];
                                if (tmp > 0)
                                {
                                    _yin[tau] *= tau / tmp;
                                }
                            }

                            // Find minimum in Yin function within tau range
                            float minYin = float.MaxValue;
                            int bestTau = 0;

                            for (int tau = tauMin; tau <= tauMax; tau++)
                            {
                                if (_yin[tau] < minYin)
                                {
                                    minYin = _yin[tau];
                                    bestTau = tau;
                                }
                            }

                            // Convert tau to frequency
                            if (bestTau > 0 && minYin < 1.0f)
                            {
                                float pitch = _sampleRate / bestTau;
                                float confidence = 1.0f - minYin;

                                // Apply confidence threshold
                                if (confidence < 0.1f)
                                {
                                    return (0f, 0f);
                                }

                                return (pitch, confidence);
                            }
                        }
                        finally
                        {
                            noWindow.Dispose();
                        }
                    }
                }
                finally
                {
                    sqrMag.Dispose();
                }
            }

            return (0f, 0f);
        }

        #endregion

        #region Window and weighting initialization

        NativeArray<float> _W;
        NativeArray<float> _spectralWeights;
        NativeArray<float> _yin;
        float _sampleRate;

        void InitializeHanningWindow()
        {
            _W = PersistentMemory.New<float>(_N);
            for (var i = 0; i < _N; i++)
                _W[i] = (1 - math.cos(2 * math.PI * i / (_N - 1))) / 2;
        }

        void InitializeSpectralWeights()
        {
            _spectralWeights = PersistentMemory.New<float>(_N / 2 + 1);
            int j = 1;

            for (int i = 0; i < _spectralWeights.Length; i++)
            {
                float freq = (float)i / (float)_N * _sampleRate;

                // Find the frequency bin in the mask
                while (j < FreqsMask.Length && freq > FreqsMask[j])
                {
                    j++;
                }

                // Interpolate between frequency points
                float a0 = YinFFTWeights[j - 1];
                float f0 = FreqsMask[j - 1];
                float a1 = YinFFTWeights[j];
                float f1 = FreqsMask[j];

                float weight;
                if (math.abs(f1 - f0) < 0.0001f)
                {
                    weight = a0;
                }
                else if (f0 == 0)
                {
                    weight = (a1 - a0) / f1 * freq + a0;
                }
                else
                {
                    weight = (a1 - a0) / (f1 - f0) * freq +
                             (a0 - (a1 - a0) / (f1 / f0 - 1));
                }

                // Interpolate in dB space, then convert to linear
                // Using 10^(dB/40) because original uses db2lin(weight/2)
                _spectralWeights[i] = math.pow(10.0f, weight / 40.0f);
            }
        }

        #endregion

        #region Private members

        readonly int _N;
        readonly int _logN;
        NativeArray<float> _I;
        NativeArray<float> _O;

        #endregion

        #region Bit-reversal permutation table

        NativeArray<int2> _P;

        void BuildPermutationTable()
        {
            _P = PersistentMemory.New<int2>(_N / 2);
            for (var i = 0; i < _N; i += 2)
                _P[i / 2] = math.int2(Permutate(i), Permutate(i + 1));
        }

        int Permutate(int x)
          => Enumerable.Range(0, _logN)
             .Aggregate(0, (a, i) => a += ((x >> i) & 1) << (_logN - 1 - i));

        #endregion

        #region Precalculated twiddle factors

        struct TFactor
        {
            public int2 I;
            public float2 W;

            public int i1 => I.x;
            public int i2 => I.y;

            public float4 W4
              => math.float4(W.x, math.sqrt(1 - W.x * W.x),
                             W.y, math.sqrt(1 - W.y * W.y));
        }

        NativeArray<TFactor> _T;

        void BuildTwiddleFactors()
        {
            _T = PersistentMemory.New<TFactor>((_logN - 1) * (_N / 4));

            var i = 0;
            for (var m = 4; m <= _N; m <<= 1)
            {
                var alpha = -2 * math.PI / m;
                for (var k = 0; k < _N; k += m)
                    for (var j = 0; j < m / 2; j += 2)
                        _T[i++] = new TFactor
                        {
                            I = math.int2((k + j) / 2, (k + j + m / 2) / 2),
                            W = math.cos(alpha * math.float2(j, j + 1))
                        };
            }
        }

        #endregion

        #region First pass job

        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct FirstPassJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> I;
            [ReadOnly] public NativeArray<float> W;
            [ReadOnly] public NativeArray<int2> P;
            [WriteOnly] public NativeArray<float4> X;

            public void Execute(int i)
            {
                var i1 = P[i].x;
                var i2 = P[i].y;
                var a1 = I[i1] * W[i1];
                var a2 = I[i2] * W[i2];
                X[i] = math.float4(a1 + a2, 0, a1 - a2, 0);
            }
        }

        #endregion

        #region DFT pass job

        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct DftPassJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<TFactor> T;
            [NativeDisableParallelForRestriction] public NativeArray<float4> X;

            static float4 Mulc(float4 a, float4 b)
              => a.xxzz * b.xyzw + math.float4(-1, 1, -1, 1) * a.yyww * b.yxwz;

            public void Execute(int i)
            {
                var t = T[i];
                var e = X[t.i1];
                var o = Mulc(t.W4, X[t.i2]);
                X[t.i1] = e + o;
                X[t.i2] = e - o;
            }
        }

        #endregion

        #region Postprocess Job

        [Unity.Burst.BurstCompile(CompileSynchronously = true)]
        struct PostprocessJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float4> X;
            [WriteOnly] public NativeArray<float2> O;
            public float DivN;
            public float DivR;
            public float F;

            public void Execute(int i)
            {
                var x = X[i];
                var l = math.float2(math.length(x.xy), math.length(x.zw));
                O[i] = (MathUtils.dBFS(l * DivN) - F) * DivR;
            }
        }

        #endregion
    }
}