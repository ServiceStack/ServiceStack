/* This reusable script is copyrighted.
   Copyright (c) 2004,2005,2006 Claude Ostyn
This script is free for use with attribution
under the Creative Commons Attribution-ShareAlike 2.5 License.
To view a copy of this license, visit
http://creativecommons.org/licenses/by-sa/2.5/
or send a letter to
Creative Commons, 559 Nathan Abbott Way, Stanford, California 94305, USA.

For any other use, contact Claude Ostyn via tools@Ostyn.com.

USE AT YOUR OWN RISK!
THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHOR OR COPYRIGHT HOLDER
BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

function centisecsToISODuration(n, bPrecise)
{
  // Note: SCORM and IEEE 1484.11.1 require centisec precision
  // Parameters:
  // n = number of centiseconds
  // bPrecise = optional parameter; if true, duration will
  // be expressed without using year and/or month fields.
  // If bPrecise is not true, and the duration is long,
  // months are calculated by approximation based on average number
  // of days over 4 years (365*4+1), not counting the extra days
  // for leap years. If a reference date was available,
  // the calculation could be more precise, but becomes complex,
  // since the exact result depends on where the reference date
  // falls within the period (e.g. beginning, end or ???)
  // 1 year ~ (365*4+1)/4*60*60*24*100 = 3155760000 centiseconds
  // 1 month ~ (365*4+1)/48*60*60*24*100 = 262980000 centiseconds
  // 1 day = 8640000 centiseconds
  // 1 hour = 360000 centiseconds
  // 1 minute = 6000 centiseconds
  var str = "P";
  var nCs=n;
  var nY=0, nM=0, nD=0, nH=0, nMin=0, nS=0;
  n = Math.max(n,0); // there is no such thing as a negative duration
  var nCs = n;
  // Next set of operations uses whole seconds
  with (Math)
  {
    nCs = round(nCs);
    if (bPrecise == true)
    {
      nD = floor(nCs / 8640000);
    }
    else
    {
      nY = floor(nCs / 3155760000);
      nCs -= nY * 3155760000;
      nM = floor(nCs / 262980000);
      nCs -= nM * 262980000;
      nD = floor(nCs / 8640000);
    }
    nCs -= nD * 8640000;
    nH = floor(nCs / 360000);
    nCs -= nH * 360000;
    var nMin = floor(nCs /6000);
    nCs -= nMin * 6000
  }
  // Now we can construct string
  if (nY > 0) str += nY + "Y";
  if (nM > 0) str += nM + "M";
  if (nD > 0) str += nD + "D";
  if ((nH > 0) || (nMin > 0) || (nCs > 0))
  {
    str += "T";
    if (nH > 0) str += nH + "H";
    if (nMin > 0) str += nMin + "M";
    if (nCs > 0) str += (nCs / 100) + "S";
  }
  if (str == "P") str = "PT0H0M0S";
  // technically PT0S should do but SCORM test suite assumes longer form.
  return str;
}


function ISODurationToCentisec(str)
{
  // Only gross syntax check is performed here
  // Months calculated by approximation based on average number
  // of days over 4 years (365*4+1), not counting the extra days
  // in leap years. If a reference date was available,
  // the calculation could be more precise, but becomes complex,
  // since the exact result depends on where the reference date
  // falls within the period (e.g. beginning, end or ???)
  // 1 year ~ (365*4+1)/4*60*60*24*100 = 3155760000 centiseconds
  // 1 month ~ (365*4+1)/48*60*60*24*100 = 262980000 centiseconds
  // 1 day = 8640000 centiseconds
  // 1 hour = 360000 centiseconds
  // 1 minute = 6000 centiseconds
  var aV = new Array(0,0,0,0,0,0);
  var bErr = false;
  var bTFound = false;
  if (str.indexOf("P") != 0) bErr = true;
  if (!bErr)
  {
    var aT = new Array("Y","M","D","H","M","S")
    var p=0, i=0;
    str = str.substr(1); //get past the P
    for (i = 0 ; i < aT.length; i++)
    {
      if (str.indexOf("T") == 0)
      {
        str = str.substr(1);
        i = Math.max(i,3);
        bTFound = true;
      }
      p = str.indexOf(aT[i]);
      //alert("Checking for " + aT[i] + "\nstr = " + str);
      if (p > -1)
      {
        // Is this a M before or after T?
        if ((i == 1) && (str.indexOf("T") > -1) && (str.indexOf("T") < p)) continue;
        if (aT[i] == "S")
        {
          aV[i] = parseFloat(str.substr(0,p))
        }
        else
        {
          aV[i] = parseInt(str.substr(0,p))
        }
        if (isNaN(aV[i]))
        {
          bErr = true;
          break;
        }
        else if ((i > 2) && (!bTFound))
        {
          bErr = true;
          break;
        }
        str = str.substr(p+1);
      }
    }
    if ((!bErr) && (str.length != 0)) bErr = true;
    //alert(aV.toString())
  }
  if (bErr)
  {
    //alert("Bad format: " + str)
    return
  }
  return aV[0]*3155760000 + aV[1]*262980000
    + aV[2]*8640000 + aV[3]*360000 + aV[4]*6000
    + Math.round(aV[5]*100)
}

// Legacy functions to translate to/from SCORM 1.2 format

function SCORM12DurationToCs(str)
{
  // Format is [HH]HH:MM:SS[.SS] or maybe sometimes MM:SS[.SS]
  // Does not catch all possible errors
  // First convert to centisecs
  var a=str.split(":");
  var nS=0, n=0;
  var nMult = 1;
  var bErr = ((a.length < 2) || (a.length > 3));
  if (!bErr)
  {
    for (i=a.length-1;i >= 0; i--)
    {
      n = parseFloat(a[i]);
      if (isNaN(n))
      {
        bErr = true;
        break;
      }
      nS += n * nMult;
      nMult *= 60;
    }
  }
  if (bErr)
  {
    alert ("Incorrect format: " + str + "\n\nFormat must be [HH]HH:MM:SS[.SS]");
    return NaN;
  }
  return Math.round(nS * 100);
}

function centisecsToSCORM12Duration(n)
{
  // Format is [HH]HH:MM:SS[.SS]
  var bTruncated = false;
  with (Math)
  {
    n = round(n);
    var nH = floor(n / 360000);
    var nCs = n - nH * 360000;
    var nM = floor(nCs / 6000);
    nCs = nCs - nM * 6000;
    var nS = floor(nCs / 100);
    nCs = nCs - nS * 100;
  }
  if (nH > 9999)
  {
    nH = 9999;
    bTruncated = true;
  }
  var str = "0000" + nH + ":";
  str = str.substr(str.length-5,5);
  if (nM < 10) str += "0";
  str += nM + ":";
  if (nS < 10) str += "0";
  str += nS;
  if (nCs > 0)
  {
    str += ".";
    if (nCs < 10) str += "0";
    str += nCs;
  }
  //if (bTruncated) alert ("Hours truncated to 9999 to fit HHHH:MM:SS.SS format")
  return str;
}


/*** time stamp helper function. Returns a time stamp in ISO format ***/

function MakeISOtimeStamp(objSrcDate, bRelative, nResolution)
{
  // Make an ISO 8601 time stamp string as specified for SCORM 2004
  // * objDate is an optional ECMAScript Date object;
  //   if objDate is null, "this instant" is assumed.
  // * bRelative is optional; if bRelative is true,
  //   the time stamp will show local time with a time offset from UTC;
  //   otherwise the time stamp will show UTC (a.k.a. Zulu) time.
  // * nResolution is optional; it specifies max decimal digits
  //   for fractions of second; it can be null, 0 or 2. If null, 2 is assumed.
  var s = "";
  var nY=0, nM=0, nD=0, nH=0, nMin=0, nS=0, nMs=0, nCs = 0;
  var bCentisecs =  ((isNaN(nResolution)) || (nResolution != 0));
  // Need to make a copy of the source date object because we will
  // tweak it if we need to round up to next second
  objDate = new Date();
  with (objDate)
  {
    setTime(objSrcDate.getTime());
    ((bRelative)? nMs = getMilliseconds(): nMs = getUTCMilliseconds());

    if (bCentisecs)
    {
      // Default precision is centisecond. Let us see whether we need to add
      // a rounding up adjustment
      if (nMs > 994)
      {
        ((bRelative)? setMilliseconds(1000): setUTCMilliseconds(1000));
      }
      else
      {
        nCs = Math.floor(nMs / 10);
      }
    }
    else
    {
      // Precision is whole seconds; round up if necessary
      if (nMs > 499)
      {
        ((bRelative)? setMilliseconds(1000): setUTCMilliseconds(1000));
      }
    }
    if (bRelative)
    {
      nY = getFullYear();
      nM = getMonth();
      nD = getDate();
      nH = getHours();
      nMin = getMinutes();
      nS = getSeconds();
    }
    else
    {
      nY = getUTCFullYear();
      nM = getUTCMonth();
      nD = getUTCDate();
      nH = getUTCHours();
      nMin = getUTCMinutes();
      nS = getUTCSeconds();
    }
  }
  // Note: Date.Month() and Date.UTCMonth() are base 0 not 1
  s = nY + "-" +
    ZeroPad(nM+1, 2) + "-" +
    ZeroPad(nD, 2) + "T" +
    ZeroPad(nH, 2) + ":" +
    ZeroPad(nMin, 2) + ":" +
    ZeroPad(nS,2);
  if (nCs > 0)
  {
    s += "." + ZeroPad(nCs,2);
  }
  if (bRelative)
  {
    // Need to flip the sign of the time zone offset
    var nTZOff = -objDate.getTimezoneOffset();
    if (nTZOff >= 0) s += "+";
    s += ZeroPad(Math.round(nTZOff / 60), 2);
    nTZOff = nTZOff % 60;
    if (nTZOff > 0) s += ":" +  ZeroPad(nTZOff, 2);
  }
  else
  {
    s += "Z";
  }
  return s;
}

function ZeroPad(n, nLength)
{
  // Takes a number and pads it with leading 0 to the length specified.
  // The padded length does not include negative sign if present.
  var bNeg = (n < 0);
  var s = n.toString();
  if (bNeg) s = s.substr(1,s.length);
  while (s.length < nLength) s = "0" + s;
  if (bNeg) s = "-" + s;
  return s
}

function trim(s)
{
  if (s == null) return "";
  return s.replace(/^\s*(\b.*\b|)\s*$/, "$1");
}

var gsParseErr = "";

function DateFromISOString(strDate)
{
  // Convert an ISO 8601 formatted string to a local date
  // Returns an ECMAScript Date object or null if an error was detected
  // Assumes that the string is well formed and SCORM conformant
  // otherwise a runtime error may occur in this function.
  // In practice the data range is limited to the date range supported
  // by the ECMAScript Date object. See the ECMAScript standard for details.
  var sDate = strDate; // The date part of the input, after a little massaging
  var sTime = null; // The time part of the input, if it is included
  var sTimeOffset = null; // UTC offset, if specified in the input string
  var sUTCOffsetSign = "";
  var a = null; // Will be reused for all kinds of string splits
  var n=0, nY=0, nM=0, nD=1, nH=0, nMin=0, nS=0, nMs = 0;

  gsParseErr = "";

  // If this is "Zulu" time, it will make things a little easier
  var bZulu = (sDate.indexOf("Z") > -1);
  if (bZulu) sDate = sDate.substr(0, sDate.length - 1);

  // Parse the ISO string into date and time
  if (sDate.indexOf("T") > -1)
  {
    var a = sDate.split("T");
    sDate = a[0];
    var sTime = a[1];
  }
  // Parse the date part
  a = sDate.split("-");
  nY = parseInt(a[0],10);
  if ((isNaN(nY)) || (nY > 9999) || (nY < 0000))
  {
    gsParseErr = "Invalid year value:\n" +  strDate;
    return null;
  }
  if (a.length > 1)
  {
    nM = parseInt(a[1],10) - 1; // months are in base 0
    if (nM < 0) alert("a[1] =" + a[1] + " from " + strDate);
    if (a.length > 2)
    {
      nD = parseInt(a[2],10); // days are in base 1
    }
  }
  // Done with the date. If there is a time part, parse it out.
  if (sTime)
  {
    if (sTime.indexOf("-") > -1) sUTCOffsetSign = "-";
    if (sTime.indexOf("+") > -1) sUTCOffsetSign = "+";
    if (sUTCOffsetSign != "")
    {
      if (bZulu)
      {
        gsParseErr = "You can't have both UTC offset and Zulu in ISO time stamp:\n" + strDate;
         return null;
      }
      a = sTime.split(sUTCOffsetSign);
      sTime = a[0];
      sTimeOffset = a[1];
    }
    a = sTime.split(":");
    nH = parseInt(a[0],10);
    if (a.length > 1)
    {
      nMin = parseInt(a[1],10);
      if (a.length > 2)
      {
        (a[2].indexOf(".")<0?nS = parseInt(a[2],10) : nS = parseFloat(a[2]));
        if (isNaN(nS))
        {
          gsParseErr = "Error parsing seconds: " + a[2] + "\n" + strDate;
          return null;
        }
        nMs = Math.round((nS % 1) * 1000);
        nS = Math.floor(nS);
      }
    }
  }
  else if (bZulu)
  {
    gsParseErr = "UTC not allowed in time stamp unless there is a Time part:\n" +
      strDate;
    return null;
  }
  var objDate = new Date();
  if (bZulu)
  {
    objDate.setUTCFullYear(nY,nM,nD);
    objDate.setUTCHours(nH,nMin,nS,nMs);
  }
  else
  {
    // Calculate and apply the time offset for local time
    if (sTimeOffset)
    {
      var nOffset = 0;
      a = sTimeOffset.split(":");
      nOffset = parseInt(a[0]);
      if (isNaN(nOffset))
      {
        gsParseErr = "Found UTC time offset but offset value is NaN:\n" + strDate;
        return null;
      }
      nOffset = nOffset * 60
      if (a.length > 1)
      {
        n = parseInt(a[1]);
        if (isNaN(n))
        {
          gsParseErr = "Found UTC time offset minutes but minute value is NaN:\n" +
            strDate;
          return null;
        }
        nOffset += n;
      }
      nOffset = nOffset * 60; // minutes to milliseconds
      if (sUTCOffsetSign == "-") nOffset = -nOffset;
      objDate.setTime(objDate.getTime() + nOffset);
    }
    objDate.setFullYear(nY,nM,nD);
    objDate.setHours(nH,nMin,nS,nMs);
  }
  return objDate
}
