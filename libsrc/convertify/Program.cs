using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using ImageMagick;
using System.ComponentModel;

namespace Convertify
{
   public class Info
   {
      public FileInfo file;
      public int index;
   };

   class Program
   {

      static string targetPath;

      static void convertify(string path)
      {
         Stopwatch timer = new Stopwatch();
         List<Info> files = new List<Info>();

         Console.WriteLine("Finding all HEIC files in {0}", path);
         foreach (string entry in System.IO.Directory.EnumerateFileSystemEntries(path, "*.heic", SearchOption.AllDirectories))
         {
            Info i = new Info();
            i.file = new FileInfo(entry);
            i.index = files.Count;
            files.Add(i);
         }

         timer.Stop();
         Console.WriteLine("Finished gathering files in {0}.  Found {1} HEIC files", timer.Elapsed.ToString("mm\\:ss\\.ff"), files.Count);
         timer.Reset();


         Console.WriteLine("Converting files to JPG");
         timer.Start();
         int completed = 0;
         Parallel.ForEach(files, (info) =>
         {
            convertFile(info.file.FullName);
            System.Threading.Interlocked.Increment(ref completed);

            if(completed % 10 == 0)
            {
               Console.Write("\rFiles Converted: {0}/{1} ({2:0.0}%)                ", completed, files.Count, (float)completed / (float)files.Count * 100.0f);
            }
         }
         );
         timer.Stop();
         Console.WriteLine();
         Console.WriteLine("Finished converting {0} files in {1}", files.Count, timer.Elapsed.ToString("mm\\:ss\\.ff"));
      }

      static void convertFile(string filePath)
      {
         string destination = System.IO.Path.ChangeExtension(filePath, ".jpg");
         using(var image = new MagickImage(filePath))
         {
            image.Write(destination);
         }
      }


      static void Main(string[] args)
      {
         if (args.Length < 1)
         {
            Console.WriteLine("Convertify usage: ");
            Console.WriteLine("  convertify [target path]");

            return;
         }

         targetPath = args[0];

         convertify(targetPath);
      }
   }
}
