using System;
using System.IO;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Html
{
  /* https://github.com/extnet/Ext.NET.Utilities/blob/master/Ext.Net.Utilities/JavaScript/JSMin.cs
   * Originally written in 'C', this code has been converted to the C# language.
   * The author's copyright message is reproduced below.
   * All modifications from the original to C# are placed in the public domain.
   */

  /* Updated to version of 2013-03-29 on 2016-09-01 by Rob Schoenaker

  /* jsmin.c
   2013-03-29
    Copyright (c) 2002 Douglas Crockford  (www.crockford.com)
    Permission is hereby granted, free of charge, to any person obtaining a copy of
    this software and associated documentation files (the "Software"), to deal in
    the Software without restriction, including without limitation the rights to
    use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
    of the Software, and to permit persons to whom the Software is furnished to do
    so, subject to the following conditions:
    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.
    The Software shall be used for Good, not Evil.
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
  public class JSMinifier : ICompressor
  {
    const int EOF = -1;

    TextReader sr;
    StringBuilder sb;
    int theA;
    int theB;
    int theLookahead = EOF;
    int theX = EOF;
    int theY = EOF;

    public string Compress(string js)
    {
      using (sr = new StringReader(js))
      {
        sb = StringBuilderCache.Allocate();
        jsmin();
        return StringBuilderCache.ReturnAndFree(sb); // return the minified string  
      }
    }

    public static string MinifyJs(string js, bool ignoreErrors = false) //removed the out file path  
    {
      string result = js;
      try
      {
        result = new JSMinifier().Compress(js);
      }
      catch (Exception ex)
      {
        if (!ignoreErrors)
        {
          throw;
        }
      }
      return result;
    }

    /* jsmin -- Copy the input to the output, deleting the characters which are 
            insignificant to JavaScript. Comments will be removed. Tabs will be 
            replaced with spaces. Carriage returns will be replaced with linefeeds. 
            Most spaces and linefeeds will be removed. 
    */
    void jsmin()
    {
      theA = '\n';
      action(3);
      while (theA != EOF)
      {
        switch (theA)
        {
          case ' ':
            {
              if (isAlphanum(theB))
              {
                action(1);
              }
              else
              {
                action(2);
              }
              break;
            }
          case '\n':
            {
              switch (theB)
              {
                case '{':
                case '[':
                case '(':
                case '+':
                case '-':
                  {
                    action(1);
                    break;
                  }
                case ' ':
                  {
                    action(3);
                    break;
                  }
                default:
                  {
                    if (isAlphanum(theB))
                    {
                      action(1);
                    }
                    else
                    {
                      action(2);
                    }
                    break;
                  }
              }
              break;
            }
          default:
            {
              switch (theB)
              {
                case ' ':
                  {
                    if (isAlphanum(theA))
                    {
                      action(1);
                      break;
                    }
                    action(3);
                    break;
                  }
                case '\n':
                  {
                    switch (theA)
                    {
                      case '}':
                      case ']':
                      case ')':
                      case '+':
                      case '-':
                      case '"':
                      case '\'':
                        {
                          action(1);
                          break;
                        }
                      default:
                        {
                          if (isAlphanum(theA))
                          {
                            action(1);
                          }
                          else
                          {
                            action(3);
                          }
                          break;
                        }
                    }
                    break;
                  }
                default:
                  {
                    action(1);
                    break;
                  }
              }
              break;
            }
        }
      }
    }
    /* action -- do something! What you do is determined by the argument: 
            1   Output A. Copy B to A. Get the next B. 
            2   Copy B to A. Get the next B. (Delete A). 
            3   Get the next B. (Delete B). 
       action treats a string as a single character. Wow! 
       action recognizes a regular expression if it is preceded by ( or , or =. 
    */
    void action(int d)
    {
      if (d <= 1)
      {
        put(theA);
        if (
            (theY == '\n' || theY == ' ') &&
            (theA == '+' || theA == '-' || theA == '*' || theA == '/') &&
            (theB == '+' || theB == '-' || theB == '*' || theB == '/')
        )
        {
          put(theY);
        }
      }

      if (d <= 2)
      {
        theA = theB;
        if (theA == '\'' || theA == '"' || theA == '`')
        {
          for (;;)
          {
            put(theA);
            theA = get();
            if (theA == theB)
            {
              break;
            }

            if (theA == '\\')
            {
              put(theA);
              theA = get();
            }
            if (theA == EOF)
            {
              throw new Exception(string.Format("Error: JSMIN unterminated string literal: {0}", theA));
            }
          }
        }
      }
      if (d <= 3)
      {
        theB = next();
        if (theB == '/' && (
            theA == '(' || theA == ',' || theA == '=' || theA == ':' ||
            theA == '[' || theA == '!' || theA == '&' || theA == '|' ||
            theA == '?' || theA == '+' || theA == '-' || theA == '~' ||
            theA == '*' || theA == '/' || theA == '{' || theA == '\n'
        ))
        {
          put(theA);
          if (theA == '/' || theA == '*')
          {
            put(' ');
          }
          put(theB);

          for (;;)
          {
            theA = get();
            if (theA == '[')
            {
              for (;;)
              {
                put(theA);
                theA = get();
                if (theA == ']')
                {
                  break;
                }
                if (theA == '\\')
                {
                  put(theA);
                  theA = get();
                }
                if (theA == EOF)
                {
                  throw new Exception("Unterminated set in Regular Expression literal.");

                }
              }
            }
            else if (theA == '/')
            {
              switch (peek())
              {
                case '/':
                case '*':
                  throw new Exception("Unterminated Regular Expression literal.");
              }
              break;
            }
            else if (theA == '\\')
            {
              put(theA);
              theA = get();
            }
            if (theA == EOF)
            {
              throw new Exception("Unterminated Regular Expression literal.");
            }
            put(theA);
          }
          theB = next();
        }
      }
    }
    /* next -- get the next character, excluding comments. peek() is used to see 
            if a '/' is followed by a '/' or '*'. 
    */
    int next()
    {
      int c = get();
      if (c == '/')
      {
        switch (peek())
        {
          case '/':
            {
              for (;;)
              {
                c = get();
                if (c <= '\n')
                {
                  break;
                }
              }
              break;
            }
          case '*':
            {
              get();
              while (c != ' ')
              {
                switch (get())
                {
                  case '*':
                    {
                      if (peek() == '/')
                      {
                        get();
                        c = ' ';
                      }
                      break;
                    }
                  case EOF:
                    {
                      throw new Exception("Error: JSMIN Unterminated comment.\n");
                    }
                }
              }
              break;
            }

        }
      }
      theY = theX;
      theX = c;
      return c;
    }
    /* peek -- get the next character without getting it. 
    */
    int peek()
    {
      theLookahead = get();
      return theLookahead;
    }
    /* get -- return the next character from stdin. Watch out for lookahead. If 
            the character is a control character, translate it to a space or 
            linefeed. 
    */
    int get()
    {
      int c = theLookahead;
      theLookahead = EOF;
      if (c == EOF)
      {
        c = sr.Read();
      }
      if (c >= ' ' || c == '\n' || c == EOF)
      {
        return c;
      }
      if (c == '\r')
      {
        return '\n';
      }
      return ' ';
    }

    void put(int c)
    {
      sb.Append((char)c);
    }
    /* isAlphanum -- return true if the character is a letter, digit, underscore, 
            dollar sign, or non-ASCII character. 
    */
    bool isAlphanum(int c)
    {
      return ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
              (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || c == '\\' ||
              c > 126);
    }
  }
}