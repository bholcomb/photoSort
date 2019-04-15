using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Data.HashFunction;
using System.Data.HashFunction.xxHash;

namespace deduplicator
{
   public class Info
   {
      public FileInfo file;
      public DateTime dateTime;
      public bool toDelete;
      public UInt64 hash;
   }

   class Program
   {
      static string targetPath;
      static IxxHash xxHash = xxHashFactory.Instance.Create();

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
         String fullPath = System.IO.Path.Combine(targetPath, path);
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

      static void hashFile(Info info)
      {
         if(info.hash != 0)
            return;

         byte[] bytes = File.ReadAllBytes(info.file.FullName);
         byte[] hashbytes = xxHash.ComputeHash(bytes, 64).Hash;
         info.hash = BitConverter.ToUInt64(hashbytes, 0);
      }

      static void deduplicate(string path)
      {
         Stopwatch timer = new Stopwatch();
         List<Info> files = new List<Info>();
         

         //find all files
         timer.Start();
         foreach(string entry in System.IO.Directory.EnumerateDirectories(path))
         {
            if(entry.EndsWith("duplicates"))
            {
               continue;
            }
            else
            {
               deduplicate(entry);
            }
         }

         foreach(string entry in Directory.EnumerateFiles(path, "*.*"))
         {
            Info i = new Info();
            i.file = new FileInfo(entry);
            files.Add(i);
         }

         List<Info> toRemove = new List<Info>();
         foreach(Info i in files)
         {
            if(i.toDelete == true)
               continue;  //this file is already marked for deletion

            foreach(Info j in files)
            {
               if(i.file.FullName == j.file.FullName)
                  continue; //don't evaluate self

               if(i.file.Length == j.file.Length)
               {
                  hashFile(i);
                  hashFile(j);
                  if(i.hash == j.hash)
                  {
                     Console.WriteLine("Duplicates {0} and {1}", i.file.FullName, j.file.FullName);
                     j.toDelete = true;
                     toRemove.Add(j);
                  }
               }
            }
         }

         bulkMove(toRemove, "duplicates");
         timer.Stop();

         Console.WriteLine("Finished directory {0} in {1}", path, timer.Elapsed.ToString("mm\\:ss\\.ff"));
      }

      static void Main(string[] args)
      {
         if(args.Length < 1)
         {
            Console.WriteLine("DeDuplicator usage: ");
            Console.WriteLine("  deduplicate [target path]");

            return;
         }

         targetPath = args[0];

         deduplicate(targetPath);
      }
   }
}
