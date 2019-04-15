# photoSort
Tools for bulk sorting of photos and movies

I had multiple folders on my various different machines filled with dumps of pictures from my phone and cameras.  I've always had the good intentions of taking the time to go through them and sort them by date or activity and archive them.  After almost 20 years, the photo pile kept growing and the task became nearly impossible to do by hand.  

I wrote the photoSorter to go through my unsorted photo (and video) folders determine the date the picture was taken based off of its metadata and move it to a directory scheme of YYYY/MM/.  I look for duplicate files (same name/size) and move the duplicates to a /duplciates directory.  I also wrote a program that will go through and get rid of any duplicates in the sorted folder based on file size and a hash of the file contents.  

If the date of the photo is not contained in the metadata, the last used modified date or the file creation date is used, whichever is earlier.  

Both programs are command line tools and will print out how to use them when run without any arguments.

