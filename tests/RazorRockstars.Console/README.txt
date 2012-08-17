## Requires sqlite3.dll module in same /bin directory as .exe or available in the OS System $PATH

In VS.NET this is done by copying the sqlite3.dll for your architecture into your projects root path:

for 32bit pc
  - copy `\sqlite\x86\sqlite3.dll` to `\`
or for 64bit
  - copy `\sqlite\x64\sqlite3.dll` to `\`

Then go to `\sqlite3.dll` properties (in VS.NET) and change the Build Action to: 'Copy if Newer'