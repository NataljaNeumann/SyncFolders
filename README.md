
![SyncFolders-1](https://github.com/user-attachments/assets/3864175e-1b28-45eb-b56a-f95d1d338d44)  

[English](#en)  

# English
<a name="en"></a>

SyncFolders is an application that aims to help you to keep two folders or drives synchronised.
With default settings this apllication creates hidden files, about 1% of the size of original files, that allow
you to completely recover from single block failures. There are two layers of protection:

1. You keep two different drives with complete copies of files
2. Even if a drive becomes inaccessible, e.g. power failure or other problems, single block failures
and also bigger error ranges in files can be restored using additionally saved information.

The original files and additionally saved information can be verified by the application. In case
of errors the appllication will try to repair the file. If there is a second copy with same date and
same length then it will try to recover single blocks from other copy and if this fails then from additionally
saved information.

In case all mentioned measures fail, the application will also try to copy an older copy from the other
drive, which is the standard behavior of backup applications.

After all, if nothing worked, then the application will recover the available parts of the file by filling
unreadable blocks with zeros, so at least the file can be read and copied, even if not all parts of it are OK.
Many media applications can jump over these missing parts.

All of this is done automatically, so your personal family photos and videos are kept safe as good as possible.

A log is shown after completion of the operation and also saved in Documents folder for later reference.

There is no need of an installation. You can extract the archive into a subfolder of the drive that contains
photos and run it from there. Windows will prompt you to install .NET-Framework, that is all you need.

