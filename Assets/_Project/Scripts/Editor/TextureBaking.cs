using System.IO;
using UnityEditor;
using UnityEngine;

namespace SporeTribal.EditorTools
{
    public static class TextureBaking
    {
        public static Texture2D SaveTileableGreyscale(string path, float[] values, int size)
        {
            var pixels = new Color32[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                byte b = (byte)(values[i] * 255f);
                pixels[i] = new Color32(b, b, b, 255);
            }
            WritePng(path, pixels, size);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.sRGBTexture = false;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static void WritePng(string path, Color32[] pixels, int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            tex.SetPixels32(pixels);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
    }
}
