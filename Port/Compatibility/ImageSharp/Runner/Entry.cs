using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Pbm;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Qoi;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;

namespace SCUnity.ImageValidation {
public static class Entry {
    static Image<Rgba32> Pattern() {
        var image=new Image<Rgba32>(37,19);
        for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)image[x,y]=new Rgba32((byte)(x*13+y*31),(byte)(x*37+y*7),(byte)(x*3+y*59),(byte)((x+y)%5==0?0:(x+y)%3==0?127:255));
        return image;
    }
    static IImageEncoder[] Encoders()=>new IImageEncoder[]{
        new BmpEncoder{BitsPerPixel=BmpBitsPerPixel.Pixel32},new BmpEncoder{BitsPerPixel=BmpBitsPerPixel.Pixel2},
        new PngEncoder{ColorType=PngColorType.RgbWithAlpha},new PngEncoder{ColorType=PngColorType.Rgb,BitDepth=PngBitDepth.Bit16},
        new JpegEncoder{Quality=95,ColorType=JpegEncodingColor.YCbCrRatio420},new GifEncoder{ColorTableMode=GifColorTableMode.Local},
        new PbmEncoder(),new QoiEncoder{Channels=QoiChannels.Rgba,ColorSpace=QoiColorSpace.SrgbWithLinearAlpha},
        new TiffEncoder{BitsPerPixel=TiffBitsPerPixel.Bit32},new TgaEncoder{BitsPerPixel=TgaBitsPerPixel.Pixel32,Compression=TgaCompression.RunLength},
        new WebpEncoder{FileFormat=WebpFileFormatType.Lossless,TransparentColorMode=WebpTransparentColorMode.Preserve},
        new WebpEncoder{FileFormat=WebpFileFormatType.Lossy,Quality=90},
    };
    static void Record(Image<Rgba32> image,BinaryWriter w) {
        w.Write(image.Width);w.Write(image.Height);w.Write(image.Frames.Count);
        foreach(var frame in image.Frames) for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)w.Write(frame[x,y].PackedValue);
    }
    public static void Generate(string folder) {
        Directory.CreateDirectory(folder);int i=0;
        using(var pattern=Pattern())foreach(var encoder in Encoders())using(var s=File.Create(Path.Combine(folder,(i++).ToString("D2")+"-"+encoder.GetType().Name+".image")))pattern.Save(s,encoder);
    }
    public static Dictionary<string,bool> Run(string fixtures,string output) {
        var checks=new Dictionary<string,bool>();
        if(Directory.GetFiles(fixtures,"*",SearchOption.AllDirectories).Length!=183)throw new InvalidOperationException("Requires all 171 game images and 12 synthetic formats");
        Directory.CreateDirectory(output);Console.WriteLine("ImageSharp "+typeof(Image).Assembly.FullName);
        foreach(string path in Directory.GetFiles(fixtures,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal)) {
            string relative=path.Substring(fixtures.TrimEnd(Path.DirectorySeparatorChar).Length+1), name=relative.Replace('\\','_').Replace('/','_');Console.WriteLine(relative);
            using(var stream=File.OpenRead(path))using(var image=Image.Load<Rgba32>(stream))using(var w=new BinaryWriter(File.Create(Path.Combine(output,name+".rgba")))) {
                Record(image,w);
                using(var small=image.Clone(ctx=>ctx.Resize(17,11)))Record(small,w);
            }
        }
        checks.Add("all-183-images-and-resize",true);
        Generate(Path.Combine(output,"encoded"));
        checks.Add("nine-formats-twelve-encoder-modes",true);
        string asyncFolder=Path.Combine(output,"encoded-async");Directory.CreateDirectory(asyncFolder);int asyncIndex=0;
        using(var pattern=Pattern())foreach(var encoder in Encoders())pattern.SaveAsync(Path.Combine(asyncFolder,(asyncIndex++).ToString("D2")+"-"+encoder.GetType().Name+".image"),encoder).GetAwaiter().GetResult();
        foreach(var path in Directory.GetFiles(Path.Combine(output,"encoded")))if(!File.ReadAllBytes(path).SequenceEqual(File.ReadAllBytes(Path.Combine(asyncFolder,Path.GetFileName(path)))))throw new InvalidOperationException("Async encoder differs");
        checks.Add("async-file-save-equals-sync",true);
        string asyncFile=Directory.GetFiles(fixtures,"*",SearchOption.AllDirectories).OrderBy(p=>p,StringComparer.Ordinal).First();
        using(var image=Image.LoadAsync<Rgba32>(asyncFile).GetAwaiter().GetResult())using(var w=new BinaryWriter(File.Create(Path.Combine(output,"async-load.rgba"))))Record(image,w);
        checks.Add("async-file-load",true);
        using(var writer=new BinaryWriter(File.Create(Path.Combine(output,"invalid.bin"))))foreach(var data in new[]{new byte[0],new byte[]{1,2,3},new byte[]{0x89,0x50,0x4e,0x47}}) {
            try{using(var bad=Image.Load<Rgba32>(data)){}throw new InvalidOperationException("Invalid data decoded");}
            catch(Exception e) when(e is UnknownImageFormatException || e is ArgumentNullException){writer.Write(e.GetType().FullName);}
        }
        checks.Add("invalid-images-rejected",true);
        var half=typeof(Image).Assembly.GetType("SixLabors.ImageSharp.PixelFormats.HalfTypeHelper",true);
        var pack=(Func<float,ushort>)Delegate.CreateDelegate(typeof(Func<float,ushort>),half.GetMethod("Pack",BindingFlags.Static|BindingFlags.NonPublic));
        var unpack=(Func<ushort,float>)Delegate.CreateDelegate(typeof(Func<ushort,float>),half.GetMethod("Unpack",BindingFlags.Static|BindingFlags.NonPublic));
        using(var w=new BinaryWriter(File.Create(Path.Combine(output,"half.bin")))) {
            for(int i=0;i<65536;i++) {float f=unpack((ushort)i);w.Write(f);w.Write(pack(f));}
            uint seed=0x13579864;for(int i=0;i<100000;i++){seed=unchecked(seed*1664525u+1013904223u);w.Write(pack(BitConverter.ToSingle(BitConverter.GetBytes(seed),0)));}
        }
        checks.Add("all-half-bit-patterns-and-100000-floats",true);
        File.WriteAllText(Path.Combine(output,"passed.txt"),"complete");
        return checks;
    }
    public static int Main(string[] args) {try{if(args[0]=="generate")Generate(args[1]);else Run(args[0],args[1]);return 0;}catch(Exception e){Console.WriteLine(e);return 1;}}
}
}
