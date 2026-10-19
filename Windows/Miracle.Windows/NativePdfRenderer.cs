using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
namespace Miracle.Windows;
internal static class NativePdfRenderer
{
    // PDFium has process-global state. Serialize calls and close each native handle
    // in the same worker that created it, before committing any output.
    private static readonly object gate=new();
    private static bool initialized;
    public static Task<IReadOnlyList<string>> RenderAsync(string input,string directory,string format,CancellationToken token)=>Task.Run<IReadOnlyList<string>>(()=>
    {
        lock(gate)
        {
            token.ThrowIfCancellationRequested();
            if(!initialized){Init();initialized=true;}
            nint document=Load(Path.GetFullPath(input),null);
            if(document==0)throw new InvalidDataException("PDF could not be opened; damaged or password-protected files are not supported.");
            try
            {
                int count=PageCount(document);if(count is <1 or >500)throw new InvalidDataException("PDF must contain 1–500 pages.");
                var result=new List<string>();long bytes=0;ImageStudio.Initialize();
                for(int i=0;i<count;i++)
                {
                    token.ThrowIfCancellationRequested();nint page=LoadPage(document,i);if(page==0)throw new InvalidDataException("PDF page could not be loaded.");
                    try
                    {
                        double w=Width(page)/72*300,h=Height(page)/72*300;
                        if(!double.IsFinite(w)||!double.IsFinite(h)||w<1||h<1||w>20000||h>20000||w*h>80_000_000)throw new InvalidDataException("PDF page exceeds the 300-DPI pixel budget.");
                        int width=(int)Math.Ceiling(w),height=(int)Math.Ceiling(h);nint bitmap=CreateBitmap(width,height,1);
                        if(bitmap==0)throw new InvalidDataException("Unable to allocate the PDF page bitmap.");
                        try
                        {
                            Fill(bitmap,0,0,width,height,0xffffffff);Render(bitmap,page,0,0,width,height,0,1);
                            token.ThrowIfCancellationRequested();
                            int stride=Stride(bitmap);if(stride!=checked(width*4))throw new InvalidDataException("Unsupported PDF bitmap stride.");
                            byte[] pixels=new byte[checked(stride*height)];Marshal.Copy(Buffer(bitmap),pixels,0,pixels.Length);
                            using var image=new MagickImage();image.ReadPixels(pixels,new PixelReadSettings((uint)width,(uint)height,StorageType.Char,PixelMapping.BGRA));image.Density=new Density(300);image.Quality=92;
                            string path=Path.Combine(directory,"page-"+(i+1).ToString("D3")+"."+format);image.Write(path,ImageStudio.Format(format));
                            bytes+=new FileInfo(path).Length;if(bytes>2L*1024*1024*1024)throw new InvalidDataException("PDF render exceeds 2 GB output budget.");result.Add(path);
                        }
                        finally{DestroyBitmap(bitmap);}
                    }
                    finally{ClosePage(page);}
                }
                return result;
            }
            finally{CloseDocument(document);}
        }
    },token);
    internal static void Shutdown()
    {
        lock(gate){if(initialized){DestroyLibrary();initialized=false;}}
    }
    [DllImport("pdfium",EntryPoint="FPDF_DestroyLibrary",CallingConvention=CallingConvention.Cdecl)] private static extern void DestroyLibrary();
    [DllImport("pdfium",EntryPoint="FPDF_InitLibrary",CallingConvention=CallingConvention.Cdecl)] private static extern void Init();
    [DllImport("pdfium",EntryPoint="FPDF_LoadDocument",CallingConvention=CallingConvention.Cdecl)] private static extern nint Load([MarshalAs(UnmanagedType.LPUTF8Str)]string path,[MarshalAs(UnmanagedType.LPUTF8Str)]string? password);
    [DllImport("pdfium",EntryPoint="FPDF_GetPageCount",CallingConvention=CallingConvention.Cdecl)] private static extern int PageCount(nint document);
    [DllImport("pdfium",EntryPoint="FPDF_LoadPage",CallingConvention=CallingConvention.Cdecl)] private static extern nint LoadPage(nint document,int index);
    [DllImport("pdfium",EntryPoint="FPDF_GetPageWidth",CallingConvention=CallingConvention.Cdecl)] private static extern double Width(nint page);
    [DllImport("pdfium",EntryPoint="FPDF_GetPageHeight",CallingConvention=CallingConvention.Cdecl)] private static extern double Height(nint page);
    [DllImport("pdfium",EntryPoint="FPDFBitmap_Create",CallingConvention=CallingConvention.Cdecl)] private static extern nint CreateBitmap(int width,int height,int alpha);
    [DllImport("pdfium",EntryPoint="FPDFBitmap_FillRect",CallingConvention=CallingConvention.Cdecl)] private static extern void Fill(nint bitmap,int left,int top,int width,int height,uint color);
    [DllImport("pdfium",EntryPoint="FPDF_RenderPageBitmap",CallingConvention=CallingConvention.Cdecl)] private static extern void Render(nint bitmap,nint page,int x,int y,int width,int height,int rotate,int flags);
    [DllImport("pdfium",EntryPoint="FPDFBitmap_GetBuffer",CallingConvention=CallingConvention.Cdecl)] private static extern nint Buffer(nint bitmap);
    [DllImport("pdfium",EntryPoint="FPDFBitmap_GetStride",CallingConvention=CallingConvention.Cdecl)] private static extern int Stride(nint bitmap);
    [DllImport("pdfium",EntryPoint="FPDFBitmap_Destroy",CallingConvention=CallingConvention.Cdecl)] private static extern void DestroyBitmap(nint bitmap);
    [DllImport("pdfium",EntryPoint="FPDF_ClosePage",CallingConvention=CallingConvention.Cdecl)] private static extern void ClosePage(nint page);
    [DllImport("pdfium",EntryPoint="FPDF_CloseDocument",CallingConvention=CallingConvention.Cdecl)] private static extern void CloseDocument(nint document);
}
