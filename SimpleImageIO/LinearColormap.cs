using System;

namespace SimpleImageIO {
    /// <summary>
    /// Linearly interpolates between equi-distant color values.
    /// </summary>
    public class LinearColormap : IColorMap {
        /// <summary>
        /// Inferno colormap from matplotlib. Ranges from black to bright yellow. Achieves high contrast
        /// and features monotonically increasing luminance.
        /// </summary>
        public static readonly RgbColor[] Inferno = new RgbColor[] {
            RgbColor.SrgbToLinear(0.001461996f, 0.000465991f, 0.013866006f),
            RgbColor.SrgbToLinear(0.003308862f, 0.002258017f, 0.024291757f),
            RgbColor.SrgbToLinear(0.006032401f, 0.004714799f, 0.038695522f),
            RgbColor.SrgbToLinear(0.009610851f, 0.007753363f, 0.055350595f),
            RgbColor.SrgbToLinear(0.014075511f, 0.011284887f, 0.072138253f),
            RgbColor.SrgbToLinear(0.019493763f, 0.015214062f, 0.08911693f),
            RgbColor.SrgbToLinear(0.025981992f, 0.019431933f, 0.106337883f),
            RgbColor.SrgbToLinear(0.033642389f, 0.023822282f, 0.123881303f),
            RgbColor.SrgbToLinear(0.042580985f, 0.028274241f, 0.141706881f),
            RgbColor.SrgbToLinear(0.052020847f, 0.032619674f, 0.159900856f),
            RgbColor.SrgbToLinear(0.061767295f, 0.036739146f, 0.178373787f),
            RgbColor.SrgbToLinear(0.07186743f, 0.040456255f, 0.197167765f),
            RgbColor.SrgbToLinear(0.082464883f, 0.043465786f, 0.216184895f),
            RgbColor.SrgbToLinear(0.093563437f, 0.045690101f, 0.235331482f),
            RgbColor.SrgbToLinear(0.105204859f, 0.047065697f, 0.254472232f),
            RgbColor.SrgbToLinear(0.117393373f, 0.047574531f, 0.273417159f),
            RgbColor.SrgbToLinear(0.130102299f, 0.047244309f, 0.291918204f),
            RgbColor.SrgbToLinear(0.143273881f, 0.046145921f, 0.309693451f),
            RgbColor.SrgbToLinear(0.156819511f, 0.044424748f, 0.326462108f),
            RgbColor.SrgbToLinear(0.170609265f, 0.042334613f, 0.341954527f),
            RgbColor.SrgbToLinear(0.184521543f, 0.040174567f, 0.355989284f),
            RgbColor.SrgbToLinear(0.19844057f, 0.038277455f, 0.368476994f),
            RgbColor.SrgbToLinear(0.212283344f, 0.036960581f, 0.379421242f),
            RgbColor.SrgbToLinear(0.225991661f, 0.036405393f, 0.388902062f),
            RgbColor.SrgbToLinear(0.23954031f, 0.036701423f, 0.397043381f),
            RgbColor.SrgbToLinear(0.252924823f, 0.037872042f, 0.403989693f),
            RgbColor.SrgbToLinear(0.266150293f, 0.039904663f, 0.409883335f),
            RgbColor.SrgbToLinear(0.27922819f, 0.042685346f, 0.414863027f),
            RgbColor.SrgbToLinear(0.2921802f, 0.046043086f, 0.41904582f),
            RgbColor.SrgbToLinear(0.305024733f, 0.04985175f, 0.422533518f),
            RgbColor.SrgbToLinear(0.31777996f, 0.05399316f, 0.425414068f),
            RgbColor.SrgbToLinear(0.330461249f, 0.058368897f, 0.427758538f),
            RgbColor.SrgbToLinear(0.343083771f, 0.06289919f, 0.429624885f),
            RgbColor.SrgbToLinear(0.355659257f, 0.067524906f, 0.431059819f),
            RgbColor.SrgbToLinear(0.368202233f, 0.072200974f, 0.432103005f),
            RgbColor.SrgbToLinear(0.380720262f, 0.076893945f, 0.432784395f),
            RgbColor.SrgbToLinear(0.393219349f, 0.081583933f, 0.433130357f),
            RgbColor.SrgbToLinear(0.405708941f, 0.086251205f, 0.433155812f),
            RgbColor.SrgbToLinear(0.418194794f, 0.090887062f, 0.43287491f),
            RgbColor.SrgbToLinear(0.430681243f, 0.095486331f, 0.432297895f),
            RgbColor.SrgbToLinear(0.443170022f, 0.100045988f, 0.431432547f),
            RgbColor.SrgbToLinear(0.455663077f, 0.104567595f, 0.430288052f),
            RgbColor.SrgbToLinear(0.468162602f, 0.109053977f, 0.428863955f),
            RgbColor.SrgbToLinear(0.480670848f, 0.11350845f, 0.427161675f),
            RgbColor.SrgbToLinear(0.493184939f, 0.11793723f, 0.425183993f),
            RgbColor.SrgbToLinear(0.505705921f, 0.122348466f, 0.42293142f),
            RgbColor.SrgbToLinear(0.518229938f, 0.126749333f, 0.420402975f),
            RgbColor.SrgbToLinear(0.530756243f, 0.131148735f, 0.41759672f),
            RgbColor.SrgbToLinear(0.543279802f, 0.135556353f, 0.414514558f),
            RgbColor.SrgbToLinear(0.555799175f, 0.139983367f, 0.411154224f),
            RgbColor.SrgbToLinear(0.568308468f, 0.144440937f, 0.40751509f),
            RgbColor.SrgbToLinear(0.580803033f, 0.14894038f, 0.403598387f),
            RgbColor.SrgbToLinear(0.593277657f, 0.153494493f, 0.399404517f),
            RgbColor.SrgbToLinear(0.605725434f, 0.158116428f, 0.394931291f),
            RgbColor.SrgbToLinear(0.61814027f, 0.162819523f, 0.390183333f),
            RgbColor.SrgbToLinear(0.630513046f, 0.167617571f, 0.385163472f),
            RgbColor.SrgbToLinear(0.642838188f, 0.172525736f, 0.379873147f),
            RgbColor.SrgbToLinear(0.655106672f, 0.177557538f, 0.374313614f),
            RgbColor.SrgbToLinear(0.667309128f, 0.18272873f, 0.368492724f),
            RgbColor.SrgbToLinear(0.679435935f, 0.188054633f, 0.362414344f),
            RgbColor.SrgbToLinear(0.691477675f, 0.193549594f, 0.356085955f),
            RgbColor.SrgbToLinear(0.703423834f, 0.199229325f, 0.349512603f),
            RgbColor.SrgbToLinear(0.715262503f, 0.205107092f, 0.342700256f),
            RgbColor.SrgbToLinear(0.72698509f, 0.211198862f, 0.33565914f),
            RgbColor.SrgbToLinear(0.738577678f, 0.217518405f, 0.32839882f),
            RgbColor.SrgbToLinear(0.750029637f, 0.224078374f, 0.320928906f),
            RgbColor.SrgbToLinear(0.761327579f, 0.23089047f, 0.313260047f),
            RgbColor.SrgbToLinear(0.772460846f, 0.237968164f, 0.305400331f),
            RgbColor.SrgbToLinear(0.783416555f, 0.245321189f, 0.297360884f),
            RgbColor.SrgbToLinear(0.794182021f, 0.252957598f, 0.289152913f),
            RgbColor.SrgbToLinear(0.804744585f, 0.260886471f, 0.280788967f),
            RgbColor.SrgbToLinear(0.815092786f, 0.269114349f, 0.272279208f),
            RgbColor.SrgbToLinear(0.825214296f, 0.27764474f, 0.263633802f),
            RgbColor.SrgbToLinear(0.835096962f, 0.286481878f, 0.254864393f),
            RgbColor.SrgbToLinear(0.844731961f, 0.29562721f, 0.245978809f),
            RgbColor.SrgbToLinear(0.854106428f, 0.30508079f, 0.23698577f),
            RgbColor.SrgbToLinear(0.863211019f, 0.314841766f, 0.227892377f),
            RgbColor.SrgbToLinear(0.872036408f, 0.324905805f, 0.218706582f),
            RgbColor.SrgbToLinear(0.880573577f, 0.335270081f, 0.209430308f),
            RgbColor.SrgbToLinear(0.888815593f, 0.345927192f, 0.200068666f),
            RgbColor.SrgbToLinear(0.896754133f, 0.356870789f, 0.190623212f),
            RgbColor.SrgbToLinear(0.904384005f, 0.368094259f, 0.181092442f),
            RgbColor.SrgbToLinear(0.911699788f, 0.379588277f, 0.171476106f),
            RgbColor.SrgbToLinear(0.918696155f, 0.391342749f, 0.161770611f),
            RgbColor.SrgbToLinear(0.92536875f, 0.403347735f, 0.151972262f),
            RgbColor.SrgbToLinear(0.931715094f, 0.415594475f, 0.142076524f),
            RgbColor.SrgbToLinear(0.937730852f, 0.428071f, 0.132077115f),
            RgbColor.SrgbToLinear(0.943415211f, 0.440766738f, 0.121973229f),
            RgbColor.SrgbToLinear(0.948765413f, 0.453673769f, 0.111761774f),
            RgbColor.SrgbToLinear(0.953779681f, 0.466779891f, 0.101448407f),
            RgbColor.SrgbToLinear(0.958456206f, 0.48007499f, 0.091046419f),
            RgbColor.SrgbToLinear(0.962794781f, 0.493550664f, 0.080587402f),
            RgbColor.SrgbToLinear(0.966792846f, 0.507196614f, 0.070130255f),
            RgbColor.SrgbToLinear(0.970450395f, 0.521004691f, 0.059777409f),
            RgbColor.SrgbToLinear(0.973765535f, 0.534966773f, 0.049712694f),
            RgbColor.SrgbToLinear(0.976736974f, 0.549072948f, 0.0402417f),
            RgbColor.SrgbToLinear(0.979364012f, 0.563319028f, 0.032274564f),
            RgbColor.SrgbToLinear(0.981643822f, 0.577695358f, 0.026786714f),
            RgbColor.SrgbToLinear(0.983575899f, 0.592195365f, 0.02397399f),
            RgbColor.SrgbToLinear(0.985157424f, 0.606812567f, 0.024068052f),
            RgbColor.SrgbToLinear(0.986387251f, 0.621541514f, 0.027335692f),
            RgbColor.SrgbToLinear(0.987262941f, 0.636374352f, 0.034085964f),
            RgbColor.SrgbToLinear(0.987782597f, 0.651304846f, 0.044483689f),
            RgbColor.SrgbToLinear(0.987943924f, 0.666330166f, 0.057112049f),
            RgbColor.SrgbToLinear(0.987745617f, 0.68144074f, 0.071206365f),
            RgbColor.SrgbToLinear(0.987185652f, 0.69663067f, 0.086416164f),
            RgbColor.SrgbToLinear(0.986263696f, 0.711893375f, 0.102536271f),
            RgbColor.SrgbToLinear(0.984978366f, 0.727222442f, 0.119459692f),
            RgbColor.SrgbToLinear(0.983330553f, 0.742609911f, 0.137137792f),
            RgbColor.SrgbToLinear(0.981325873f, 0.758042675f, 0.155561593f),
            RgbColor.SrgbToLinear(0.978973537f, 0.773511386f, 0.174755503f),
            RgbColor.SrgbToLinear(0.976286481f, 0.789000216f, 0.194759564f),
            RgbColor.SrgbToLinear(0.973274605f, 0.804496002f, 0.215643721f),
            RgbColor.SrgbToLinear(0.969972342f, 0.819974237f, 0.237481194f),
            RgbColor.SrgbToLinear(0.966430627f, 0.835403814f, 0.260360556f),
            RgbColor.SrgbToLinear(0.962697887f, 0.850753947f, 0.284406927f),
            RgbColor.SrgbToLinear(0.958888537f, 0.865968484f, 0.309719416f),
            RgbColor.SrgbToLinear(0.955145397f, 0.880980419f, 0.336417329f),
            RgbColor.SrgbToLinear(0.951667818f, 0.89570721f, 0.364621528f),
            RgbColor.SrgbToLinear(0.948770339f, 0.910025577f, 0.394347522f),
            RgbColor.SrgbToLinear(0.946855125f, 0.923792619f, 0.425511383f),
            RgbColor.SrgbToLinear(0.946404889f, 0.936854535f, 0.45782912f),
            RgbColor.SrgbToLinear(0.94789824f, 0.949080955f, 0.490781527f),
            RgbColor.SrgbToLinear(0.951672442f, 0.960411199f, 0.523691492f),
            RgbColor.SrgbToLinear(0.957816991f, 0.970879447f, 0.555898301f),
            RgbColor.SrgbToLinear(0.96617933f, 0.980600447f, 0.586964765f),
            RgbColor.SrgbToLinear(0.976468951f, 0.989715295f, 0.616644712f),
            RgbColor.SrgbToLinear(0.98836208f, 0.998361647f, 0.644924098f),
        };

        float min, max;
        RgbColor[] stops;

        /// <summary>
        /// Generates a new colormap that maps values within the given bounds to a color
        /// </summary>
        /// <param name="min">Value mapped to the first color in the list</param>
        /// <param name="max">Value mapped to the last color in the list</param>
        /// <param name="stops">List of linear RGB colors. If null, the default <see cref="Inferno" /> is used</param>
        public LinearColormap(float min = 0.0f, float max = 1.0f, RgbColor[] stops = null) {
            this.min = min;
            this.max = max;
            this.stops = stops ?? Inferno;
        }

        /// <summary>
        /// Maps the value to its corresponding interpolated color
        /// </summary>
        /// <param name="value">Arbitrary floating point value. Will be clamped to the (min, max) range.</param>
        /// <returns>Interpolated linear RGB color</returns>
        RgbColor IColorMap.Map(float value) {
            if (value <= min) return stops[0];
            if (value >= max) return stops[^1];

            float relative = (value - min) / (max - min) * (stops.Length - 1);
            int lower = (int)relative;
            int upper = lower + 1;
            float t = relative - (int)relative;
            return t * stops[upper] + (1 - t) * stops[lower];
        }
    }
}