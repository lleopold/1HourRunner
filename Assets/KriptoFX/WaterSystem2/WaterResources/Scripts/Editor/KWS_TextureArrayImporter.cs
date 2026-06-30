#if UNITY_EDITOR
using System;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using UnityEditor.AssetImporters;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace KWS
{
    [ScriptedImporter(1, "kwsTextureArray")]
    internal class KWS_TextureArrayImporter : ScriptedImporter
    {
        const uint MAGIC   = 0x4B575341; // 'KWSA'
        const int  VERSION = 3;

        //====================================================================
        // WRITE
        //====================================================================
        public static void Write(RenderTexture rtArray, string pathNoExt)
        {
            if (rtArray == null)
                throw new ArgumentNullException(nameof(rtArray));

            if (rtArray.dimension != TextureDimension.Tex2DArray)
                throw new ArgumentException("rtArray must be Tex2DArray");

            int  width   = rtArray.width;
            int  height  = rtArray.height;
            int  slices  = rtArray.volumeDepth;
            var  format  = rtArray.graphicsFormat;
            bool hasMips = rtArray.useMipMap;

            byte[] payload = ReadbackSlices0(rtArray, width, height, slices);

            string path = pathNoExt + ".kwsTextureArray";

            using var fs = File.Open(path, FileMode.Create);
            using var gz = new GZipStream(fs, CompressionMode.Compress);
            using var bw = new BinaryWriter(gz);

            bw.Write(MAGIC);
            bw.Write(VERSION);

            bw.Write((int)format);
            bw.Write(width);
            bw.Write(height);
            bw.Write(slices);
            bw.Write(hasMips);

            bw.Write(payload.Length);
            bw.Write(payload);

            AssetDatabase.Refresh();
            Debug.Log($"KWS Saved Texture2DArray → {path}");
        }


        static byte[] ReadbackSlices0(RenderTexture rtArray, int width, int height, int slices)
        {
            int bpp       = (int)GraphicsFormatUtility.GetBlockSize(rtArray.graphicsFormat);
            int sliceSize = width * height * bpp;

            byte[] all = new byte[sliceSize * slices];

            RenderTexture prev = RenderTexture.active;
            var tmp = new RenderTexture(width, height, 0, rtArray.graphicsFormat)
            {
                useMipMap        = false,
                autoGenerateMips = false
            };
            tmp.Create();

            var cpu = new Texture2D(width, height, rtArray.graphicsFormat, TextureCreationFlags.None);

            for (int s = 0; s < slices; s++)
            {
                Graphics.CopyTexture(rtArray, s, 0, tmp, 0, 0);

                RenderTexture.active = tmp;
                cpu.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                cpu.Apply(false);

                Buffer.BlockCopy(cpu.GetRawTextureData(), 0, all, s * sliceSize, sliceSize);
            }

            RenderTexture.active = prev;
            tmp.Release();
            UnityEngine.Object.DestroyImmediate(cpu);

            return all;
        }

        //====================================================================
        // IMPORT
        //====================================================================
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using var fs = File.Open(ctx.assetPath, FileMode.Open);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var br = new BinaryReader(gz);

            uint magic = br.ReadUInt32();
            if (magic != MAGIC)
                throw new Exception("Invalid kwsTextureArray file");

            int version = br.ReadInt32();
            if (version != VERSION)
                Debug.LogWarning($"Different KWS texture version {version}");

            GraphicsFormat format      = (GraphicsFormat)br.ReadInt32();
            int            width       = br.ReadInt32();
            int            height      = br.ReadInt32();
            int            slices      = br.ReadInt32();
            bool           bakedHasMip = br.ReadBoolean();

            int    payloadSize = br.ReadInt32();
            byte[] data        = br.ReadBytes(payloadSize);

            int bpp        = (int)GraphicsFormatUtility.GetBlockSize(format);
            int sliceBytes = width * height * bpp;

            var texArray = new Texture2DArray(width, height, slices, format, bakedHasMip ? TextureCreationFlags.MipChain : TextureCreationFlags.None);

            var tmp = new Texture2D(width, height, format, TextureCreationFlags.None);

            for (int s = 0; s < slices; s++)
            {
                byte[] sliceBuf = new byte[sliceBytes];
                Buffer.BlockCopy(data, s * sliceBytes, sliceBuf, 0, sliceBytes);

                tmp.LoadRawTextureData(sliceBuf);
                tmp.Apply(false);

                texArray.SetPixels(tmp.GetPixels(), s, 0);
            }

            texArray.Apply(updateMipmaps: bakedHasMip, makeNoLongerReadable: false);

            UnityEngine.Object.DestroyImmediate(tmp);

            string name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset(name, texArray);
            ctx.SetMainObject(texArray);
        }
    }
}
#endif