using Intel.RealSense;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace RsCapture
{
    /// <summary>
    /// Representa a una captura de imagen de profundidad con todo lo necesario para poder medir distancias
    /// sobre ella
    /// </summary>
    public class DepthImage
    {
        public int Width { get; }
        public int Height { get; }
        public float DepthScale { get; }
        public UInt16[] DepthBuffer { get; private set; }
        public byte[] ColorBuffer { get; private set; }
        public byte[] ColorizedBuffer { get; private set; }
        public byte[] ConfidenceBuffer { get; private set; }
        public Intrinsics Intrinsics { get; private set; }

        public bool HasColorized => ColorizedBuffer != null;
        public bool HasConfidence => ConfidenceBuffer != null;


        public DepthImage(int w, int h, float depthScale, Intrinsics intrinsics)
        {
            Width = w;
            Height = h;
            DepthScale = depthScale;
            Intrinsics = intrinsics;
            DepthBuffer = new ushort[w * h];
        }

        public DepthImage(int w, int h, float depthScale, Intrinsics intrinsics, DepthFrame df, VideoFrame cf, VideoFrame confif = null) : this(w, h, depthScale, intrinsics)
        {
            //rellenamos
            Fill(df, cf, confif);
        }

        public DepthImage Clone()
        {
            var c = new DepthImage(Width, Height, DepthScale, Intrinsics);
            c.DepthBuffer = this.DepthBuffer.ToArray();
            if (ColorBuffer != null)
            {
                c.ColorBuffer = this.ColorBuffer.ToArray();
            }
            if (ColorizedBuffer != null)
            {
                c.ColorizedBuffer = this.ColorizedBuffer.ToArray();
            }
            if (ConfidenceBuffer != null)
            {
                c.ConfidenceBuffer = this.ConfidenceBuffer.ToArray();
            }
            return c;
        }

        public Vector3 Deproject(int x, int y) => Deproject(new Vector2(x, y));
        public Vector3 Deproject(Vector2 p)
        {
            float z = DepthBuffer[(int)p.Y * Width + (int)p.X] * 1000.0f * DepthScale;
            Debug.Assert(z >= 0);
            return new Vector3
            {
                X = z * (p.X - Intrinsics.ppx) / Intrinsics.fx,
                Y = z * (p.Y - Intrinsics.ppy) / Intrinsics.fy,
                Z = z
            };
        }

        public double Measure(Vector2 p1, Vector2 p2)
        {
            var p1d = Deproject(p1);
            var p2d = Deproject(p2);
            return Vector3.Distance(p1d, p2d);
        }

        public void Fill(DepthFrame df, VideoFrame color, VideoFrame confidence = null)
        {
            FillDepth(df);
            FillColor(color);
            if (confidence != null)
            {
                FillConfidence(confidence);
            }
        }

        public void FillDepth(DepthFrame df)
        {
            unsafe
            {
                fixed (ushort* p = DepthBuffer)
                {
                    Buffer.MemoryCopy(df.Data.ToPointer(), p, 2 * Width * Height, 2 * Width * Height);
                }
            }
        }

        public void FillColor(VideoFrame vf)
        {
            if (ColorBuffer == null)
            {
                ColorBuffer = new byte[3 * Width * Height];
            }

            unsafe
            {
                fixed (byte* p = ColorBuffer)
                {
                    Buffer.MemoryCopy(vf.Data.ToPointer(), p, ColorBuffer.Length, ColorBuffer.Length);
                }
            }
        }

        public void FillColorized(VideoFrame cf)
        {
            if (ColorizedBuffer == null)
            {
                ColorizedBuffer = new byte[3 * Width * Height];
            }

            unsafe
            {
                fixed (byte* p = ColorizedBuffer)
                {
                    Buffer.MemoryCopy(cf.Data.ToPointer(), p, ColorizedBuffer.Length, ColorizedBuffer.Length);
                }
            }
        }

        public void FillConfidence(VideoFrame cf)
        {
            if (ConfidenceBuffer == null)
            {
                ConfidenceBuffer = new byte[Width * Height];
            }

            unsafe
            {
                fixed (byte* p = ConfidenceBuffer)
                {
                    Buffer.MemoryCopy(cf.Data.ToPointer(), p, ConfidenceBuffer.Length, ConfidenceBuffer.Length);
                }
            }
        }

        public static DepthImage FromFile(string fn)
        {
            using (BinaryReader r = new BinaryReader(File.OpenRead(fn)))
            {
                var magic = r.ReadString();
                var t = r.ReadString();
                var v = r.ReadString();

                var w = r.ReadInt32();
                var h = r.ReadInt32();
                var ds = r.ReadSingle();
                var intrinsics = JsonConvert.DeserializeObject<Intrinsics>(r.ReadString());
                var di = new DepthImage(w, h, ds, intrinsics);

                if (r.ReadBoolean())
                {
                    //leemos el buffer de profundidad
                    var l = r.ReadInt32();
                    var db = r.ReadBytes(l);
                    var sdb = new ReadOnlySpan<byte>(db);
                    var usdb = MemoryMarshal.Cast<byte, ushort>(sdb);
                    di.DepthBuffer = usdb.ToArray();
                }

                if (r.ReadBoolean())
                {
                    //leemos el buffer de color
                    var l = r.ReadInt32();
                    di.ColorBuffer = r.ReadBytes(l);
                }

                if (r.ReadBoolean())
                {
                    //leemos el colorizado
                    var l = r.ReadInt32();
                    di.ColorizedBuffer = r.ReadBytes(l);
                }

                if (r.ReadBoolean())
                {
                    //leemos el buffer de confianza
                    var l = r.ReadInt32();
                    di.ConfidenceBuffer = r.ReadBytes(l);
                }

                //leemos las medidas
                var mjson = r.ReadString();
                //var ms = JsonConvert.DeserializeObject<Measure[]>(mjson, GetJsonSerializerSettings());
                //di.Measures = new(ms);

                return di;
            }
        }

        static JsonSerializerSettings GetJsonSerializerSettings() => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        };

        public void Save(string fn)
        {
            using (BinaryWriter writer = new BinaryWriter(File.Create(fn)))
            {
                writer.Write("SOVA"); //magic
                writer.Write("DI"); //type 
                writer.Write("1.0"); //version

                writer.Write(Width);
                writer.Write(Height);
                writer.Write(DepthScale);

                var jsonIntrinsics = JsonConvert.SerializeObject(Intrinsics);
                writer.Write(jsonIntrinsics);

                //depth                
                writer.Write(DepthBuffer != null);
                if (DepthBuffer != null)
                {
                    var rspan = new ReadOnlySpan<ushort>(DepthBuffer);
                    var rbspan = MemoryMarshal.Cast<ushort, byte>(rspan);
                    writer.Write(rbspan.Length);
                    writer.Write(rbspan);
                }

                writer.Write(ColorBuffer != null);
                if (ColorBuffer != null)
                {
                    writer.Write(ColorBuffer.Length);
                    writer.Write(ColorBuffer);
                }

                writer.Write(ColorizedBuffer != null);
                if (ColorizedBuffer != null)
                {
                    writer.Write(ColorizedBuffer.Length);
                    writer.Write(ColorizedBuffer);
                }

                writer.Write(ConfidenceBuffer != null);
                if (ConfidenceBuffer != null)
                {
                    writer.Write(ConfidenceBuffer.Length);
                    writer.Write(ConfidenceBuffer);
                }

                //guardamos las medidas                
                //var mjson = JsonConvert.SerializeObject(Measures.ToArray(), GetJsonSerializerSettings());
                //writer.Write(mjson);
            }
        }
    }

    public static class DepthImageExtensions
    {

    }
}
