using System;
using System.Runtime.InteropServices;

namespace Gr8scale;

internal static class MagnificationInterop
{
    private const string Dll = "Magnification.dll";

    [DllImport(Dll)]
    private static extern bool MagInitialize();

    [DllImport(Dll)]
    private static extern bool MagUninitialize();

    [DllImport(Dll)]
    private static extern bool MagSetFullscreenColorEffect(ref MAGCOLOREFFECT effect);

    [StructLayout(LayoutKind.Sequential)]
    private struct MAGCOLOREFFECT
    {
        public float m11, m12, m13, m14, m15;
        public float m21, m22, m23, m24, m25;
        public float m31, m32, m33, m34, m35;
        public float m41, m42, m43, m44, m45;
        public float m51, m52, m53, m54, m55;
    }

    private static MAGCOLOREFFECT Identity() => new()
    {
        m11 = 1, m22 = 1, m33 = 1, m44 = 1, m55 = 1,
    };

    // Lerp from identity (intensity=0) to BT.601 luminance grayscale (intensity=1).
    // intensity is clamped to [0, 1].
    private static MAGCOLOREFFECT Grayscale(double intensity)
    {
        float t = (float)Math.Clamp(intensity, 0.0, 1.0);
        float k = 1f - t;

        // BT.601 luma coefficients
        const float lr = 0.299f, lg = 0.587f, lb = 0.114f;

        return new MAGCOLOREFFECT
        {
            // Row 1: red out = k*R + t*(lr*R + lg*G + lb*B)
            m11 = k + t * lr, m12 = t * lr, m13 = t * lr, m14 = 0, m15 = 0,
            // Row 2: green out
            m21 = t * lg, m22 = k + t * lg, m23 = t * lg, m24 = 0, m25 = 0,
            // Row 3: blue out
            m31 = t * lb, m32 = t * lb, m33 = k + t * lb, m34 = 0, m35 = 0,
            // Row 4: alpha pass-through
            m41 = 0, m42 = 0, m43 = 0, m44 = 1, m45 = 0,
            // Row 5: translation
            m51 = 0, m52 = 0, m53 = 0, m54 = 0, m55 = 1,
        };
    }

    public static bool Initialize() => MagInitialize();
    public static bool Uninitialize() => MagUninitialize();

    /// <summary>Apply grayscale at the given intensity (0.0 = full color, 1.0 = full gray).</summary>
    public static bool SetIntensity(double intensity)
    {
        var eff = Grayscale(intensity);
        return MagSetFullscreenColorEffect(ref eff);
    }

    public static bool SetIdentity()
    {
        var eff = Identity();
        return MagSetFullscreenColorEffect(ref eff);
    }
}
