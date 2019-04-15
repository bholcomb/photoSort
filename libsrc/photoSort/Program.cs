using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Data.HashFunction;
using System.Data.HashFunction.xxHash;

using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Iptc;
using MetadataExtractor.Formats.Jpeg;

namespace PhotoSort
{
   public class Info
   {
      public FileInfo file;
      public DateTime dateTime;
      public UInt64 hash;
   }

   class Program
   {
      static string targetPath;
      static string destPath;

      static DateTime? GetTakenDateTime(IEnumerable<MetadataExtractor.Directory> directories)
      {
         // obtain the Exif SubIFD directory
         var directory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

         if(directory == null)
            return null;

         // query the tag's value
         if(directory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTime))
            return dateTime;

         if(directory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime2))
            return dateTime2;


         return null;
      }

      static void movefile(string destPath, Info info)
      {
         if(System.IO.Directory.Exists(destPath) == false)
         {
            System.IO.Directory.CreateDirectory(destPath);
         }

         string destFilename = Path.Combine(destPath, info.file.Name);
         if(File.Exists(destFilename) == true)
         {
            FileInfo destFile = new FileInfo(destFilename);
            bool sameSize = destFile.Length == info.file.Length;
            if(sameSize)
            {
               Console.WriteLine("File {0} already exists", destFile);
               File.Delete(info.file.FullName);
               return;
            }
            else
            {
               while(File.Exists(destFilename) == true)
               {
                  destFilename = Path.Combine(destPath, Path.GetFileNameWithoutExtension(destFilename) + "_" + info.file.Extension);
               }
            }
         }

         File.Move(info.file.FullName, destFilename);
      }

      static void bulkMove(List<Info> list, String path)
      {
         Stopwatch timer = new Stopwatch();
         String fullPath = System.IO.Path.Combine(destPath, path);
         Console.WriteLine("Moving {0} files to: {1}", list.Count, fullPath);
         timer.Start();
         for(int i = 0; i < list.Count; i++)
         {
            Info info = list[i];
            movefile(fullPath, info);

            if(i % 10 == 0)
            {
               Console.Write("\rFiles Moved: {0}/{1} ({2:0.0}%)", i, list.Count, (float)i / (float)list.Count * 100.0f);
            }
         }
         timer.Stop();
         Console.WriteLine("Finished moving {0} files in {1}", list.Count, timer.Elapsed.ToString("mm\\:ss\\.ff"));
      }


      static void findAndMoveFiles(string ext)
      {
         Stopwatch timer = new Stopwatch();
         List<Info> files = new List<Info>();
         HashSet<UInt64> fileHashes = new HashSet<ulong>();
         IxxHash xxHash = xxHashFactory.Instance.Create();
         List<Info> duplicates = new List<Info>();

         //find all files
         timer.Start();
         Console.WriteLine("Gathering image metadata: {0} ", ext);
         foreach(string entry in System.IO.Directory.EnumerateFileSystemEntries(targetPath, ext, SearchOption.AllDirectories))
         {
            Info i = new Info();
            i.file = new FileInfo(entry);

            DateTime? taken = null;
            try
            {
               // Read all metadata from the image
               var directories = ImageMetadataReader.ReadMetadata(i.file.FullName);
               taken = GetTakenDateTime(directories);
            }
            catch(ImageProcessingException e)
            {
               Console.WriteLine();
               Console.WriteLine("Image processing error with file {0}", i.file.FullName);
            }

            if(taken != null)
            {
               i.dateTime = (DateTime)taken;
            }
            else
            {
               DateTime created = i.file.CreationTime;
               DateTime modified = i.file.LastWriteTime;
               if(modified < created)
                  i.dateTime = modified;
               else
                  i.dateTime = created;
            }


            //generate hash
            string hashString = string.Format("{0}-{1}-{2}", i.file.Name, i.file.Length, i.dateTime.ToString());
            byte[] bytes = xxHash.ComputeHash(hashString, 64).Hash;
            i.hash = BitConverter.ToUInt64(bytes, 0);

            if(fileHashes.Contains(i.hash) == false)
            {
               fileHashes.Add(i.hash);
               files.Add(i);

               if(files.Count % 10 == 0)
               {
                  Console.Write("\rFiles processed: {0}  Duplicates: {1}", files.Count, duplicates.Count);
               }
            }
            else
            {
               string existingFile = "";
               foreach(Info info in files)
               {
                  if(info.hash == i.hash)
                  {
                     existingFile = info.file.FullName;
                     if(info.file.Name != i.file.Name)
                     {
                        Console.WriteLine("ERROR: Hash Collision between {0} and {1}", existingFile, i.file.FullName);
                        files.Add(i);
                        break;
                     }

                     duplicates.Add(i);
                     break;
                  }
               }
            }
         }
         Console.WriteLine("");

         timer.Stop();
         Console.WriteLine("Finished gathering metadata in {0}", timer.Elapsed.ToString("mm\\:ss\\.ff"));
         timer.Reset();

         Console.WriteLine("Moving files to: {0}", destPath);
         timer.Start();
         for(int i = 0; i < files.Count; i++)
         {
            Info info = files[i];
            string destination = System.IO.Path.Combine(destPath, info.dateTime.Year.ToString(), info.dateTime.Month.ToString());
            movefile(destination, info);
            if(i % 10 == 0)
            {
               Console.Write("\rFiles Moved: {0}/{1} ({2:0.0}%)", i, files.Count, (float)i / (float)files.Count * 100.0f);
            }
         }
         timer.Stop();
         Console.WriteLine("Finished moving {0} files in {1}", files.Count, timer.Elapsed.ToString("mm\\:ss\\.ff"));

         bulkMove(duplicates, "duplicates");
      }

      static void Main(string[] args)
      {
         if(args.Length < 2)
         {
            Console.WriteLine("PhotoSort usage: ");
            Console.WriteLine("  photoSort [target path] [destination path]");

            return;
         }

         targetPath = args[0];
         destPath = args[1];

         string[] ext = new string[] { "*.jpg", "*.mov", "*.mp4", "*.tif", "*.tiff", "*.jpeg", "*.png", "*.3gp", "*.avi", "*.mpg", "*.wmv", "*.gif", "*.bmp" };

         foreach(String s in ext)
         {
            findAndMoveFiles(s);
         }
      }
   }
}
