----------------------------------------------------------------------------
ID3-TagIT Templates
----------------------------------------------------------------------------

The ID3-TagIT Templates (itl-files) in this folder are used by ID3-TagIT to create filelists. 

These files can be edited or created with a normal text editor like notepad. They are divided into four sections:

1.) [ITLHEADER]
2.) [ITLRECORD]
3.) [ITLFOOTER]
4.) [ITLEXTENSION]

Each section begins with its name in brackets (points 1-4) and ends when the next one starts. 
Have a look at the other templates to get an overview.

1.) [ITLHEADER]
The ITLHEADER section contains the part of the future list file which has to be in front of the data of the MP3 files. When you create a HTML table for example that would be the HTML header and the first body structure till the point where the table data would be entered. You can enter multiple lines. Every line of this section is written in the future list file as it is.

2.) [ITLRECORD]
The ITLRECORD section contains the format for the data for ONE MP3 file. For each MP3 file ALL lines within this section will be written to the future list file.  Within this sections you can use the following variables which will be replaced by the information from the TAGs of the MP3 file:

%Filename%
%Subdirectory%
%Ver1%
%Ver2%
%Artist%
%Title%
%Album%
%Track%
%Position%
%Comment%
%Genre%
%Year%
%Rating%
%Duration%
%DurationSec%
%Bitrate%
%CBRVBR%
%Samplerate%
%Channel%
%Version%
%LastChanged%
%Composer%
%FileSize%
%BPM%			
%FullFilename%		- Path and Filename to list file
%RelFilename%			- Path and Filename of the MP3 file relative to list file
%CRC%							- CRC Checksum of the complete file (Audio+TAGs) (LENGTHLY OPERATION)

Keep in mind that these variables can only be used within this section.
The variables are CASE SENSITIVE.

3.) [ITLFOOTER]
The ITLFOOTER section contains the part of the future list file which has to after the data of the MP3 files. When you create a HTML table for example that would be all open HTML-tags. You can enter multiple lines. Every line of this section is written in the future list file as it is.

4.) [ITLEXTENSION]
This section only contains one line and within that line you have to enter the extension for the file without the leader dot. 
Example: html


GENERAL INFO:
ID3-TagIT does NOT check your template in regard to this point or the lines you enter for the list file. So YOU have to know how the future file has to look like (e. g. HTML file structure).
