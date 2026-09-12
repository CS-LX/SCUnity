namespace SCUnity.Compatibility
{
    // Four scalar lanes for the exact subset used by upstream matrix/vector code.
    // Keep separate float operations and lane order; do not introduce fused multiply-add.
    internal readonly unsafe struct Float4
    {
        readonly float x,y,z,w;
        public Float4(float x, float y, float z, float w)
        {
            this.x = x; this.y = y; this.z = z; this.w = w;
        }
        public float this[int index] => index switch
        {
            0 => x, 1 => y, 2 => z, 3 => w,
            _ => throw new System.ArgumentOutOfRangeException(nameof(index))
        };
        // The operations below are implemented on every target, without CPU intrinsics.
        public static bool IsSupported => true;
        public static Float4 Create(float value) => new Float4(value, value, value, value);
        public static Float4 LoadUnsafe(ref float first)
        {
            fixed (float* p = &first) return new Float4(p[0], p[1], p[2], p[3]);
        }
        public static void StoreUnsafe(Float4 value, ref float first)
        {
            fixed (float* p = &first) { p[0] = value.x; p[1] = value.y; p[2] = value.z; p[3] = value.w; }
        }
        public static Float4 Shuffle(Float4 a, Float4 b, byte mask)
            => new Float4(a[mask & 3], a[(mask >> 2) & 3], b[(mask >> 4) & 3], b[(mask >> 6) & 3]);
        public static Float4 UnpackLow(Float4 a, Float4 b) => new Float4(a.x, b.x, a.y, b.y);
        public static Float4 UnpackHigh(Float4 a, Float4 b) => new Float4(a.z, b.z, a.w, b.w);
        public static Float4 MoveHighToLow(Float4 a, Float4 b) => new Float4(b.z, b.w, a.z, a.w);
        public static Float4 Floor(Float4 a) => new Float4((float)System.Math.Floor(a.x), (float)System.Math.Floor(a.y), (float)System.Math.Floor(a.z), (float)System.Math.Floor(a.w));
        public static Float4 Ceiling(Float4 a) => new Float4((float)System.Math.Ceiling(a.x), (float)System.Math.Ceiling(a.y), (float)System.Math.Ceiling(a.z), (float)System.Math.Ceiling(a.w));
        public static Float4 Round(Float4 a) => new Float4((float)System.Math.Round(a.x), (float)System.Math.Round(a.y), (float)System.Math.Round(a.z), (float)System.Math.Round(a.w));
        public static Float4 operator +(Float4 a, Float4 b) => new Float4(a.x + b.x, a.y + b.y, a.z + b.z, a.w + b.w);
        public static Float4 operator -(Float4 a, Float4 b) => new Float4(a.x - b.x, a.y - b.y, a.z - b.z, a.w - b.w);
        public static Float4 operator *(Float4 a, Float4 b) => new Float4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
        public static Float4 operator /(Float4 a, Float4 b) => new Float4(a.x / b.x, a.y / b.y, a.z / b.z, a.w / b.w);
    }
}
