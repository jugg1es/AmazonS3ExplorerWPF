# AmazonS3ExplorerWPF
WPF-based file explorer for Amazon S3


Why?
----

Ever needed quick access to the files on your S3 account like Windows Explorer, but didn't want to pay for third party software?  Well, this might do the trick for you. 

What does it do?
----------------
- Uses the AWS profile manager so you can configure the software for any number of accounts
- Upload and download files and entire folders
- View individual files using the default program
- Create, delete, rename and copy/cut "folders" (S3 doesn't really use folders, but this application fakes it for you)
- Create, delete, rename, and copy/cut files

What does it NOT do?
--------------------

You can't actually manage your S3 account. You need to use the AWS console to create/configure buckets and user accounts.  It also won't let you set custom headers, but that might be a nice thing to add.
    
Getting Started
---------------

If you just want to start using it, get everything in the CompiledApp folder and run AmazonS3ExplorerWPF.exe.

If you want to build it yourself, this was built using Visual Studio 2015, so you'll probably need that.  The free version will probably work but I haven't tested it myself.  All of the third party libraries are included in the project (I hope I'm allowed to do that).  

Can I help?
-----------

Sure, this isn't production-ready software, but it works.  I've never actually collaborated with anyone on GitHub before but I'd love some help.  

Requirements
------------
- Windows 7 and up
- .NET 4.5
- Amazon Web Services (AWS) account
- At least one S3 bucket
    
Screenshot
----------

![alt tag](https://github.com/jugg1es/AmazonS3ExplorerWPF/blob/master/screenshot.png)
    
