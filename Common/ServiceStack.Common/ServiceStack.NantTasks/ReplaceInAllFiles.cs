using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NAnt.Core.Attributes;

namespace ServiceStack.NantTasks
{
    [TaskName("replaceInAllFiles")]
    public class ReplaceInAllFiles : ReplaceNantTask
    {
        protected override void OnEachDirectory(DirectoryInfo dirInfo
            ,IList<string> oldValues, IList<string> newValues)
        {
            var fileInfos = dirInfo.GetFiles(this.Pattern);
            foreach (var fileInfo in fileInfos)
            {
                ReplaceFileContent(fileInfo.FullName);
            }
        }

        public class FileState
        {
            public byte[] Data;
            public string FileName;
            public FileStream FileStream;
        }

        private void ReplaceFileContent(string file)
        {
            var fileStream = new FileStream(file, FileMode.Open);
            var state = new FileState
            {
                FileStream = fileStream,
                FileName = file,
                Data = new byte[fileStream.Length]
            };
            fileStream.BeginRead(state.Data, 0, (int)fileStream.Length, ReadDone, state);
        }

        private void ReadDone(IAsyncResult result)
        {
            var state = (FileState)result.AsyncState;
            var stream = state.FileStream;
            var bytesRead = stream.EndRead(result);
            stream.Close();
            if (bytesRead != state.Data.Length)
            {
                throw new ApplicationException("Invalid read:" + state.FileName);
            }
            var content = Encoding.ASCII.GetString(state.Data);
            for (var i = 0; i < OldValuesArray.Length; i++)
            {
                content = content.Replace(OldValuesArray[i], NewValuesArray[i]);
            }
            WriteContent(state.FileName, content);
        }

        private static void WriteContent(string file, string content)
        {
            var fileStream = new FileStream(file, FileMode.Truncate);
            var state = new FileState
            {
                FileStream = fileStream
            };
            var data = Encoding.ASCII.GetBytes(content);
            fileStream.BeginWrite(data, 0, data.Length, WriteDone, state);
        }

        private static void WriteDone(IAsyncResult result)
        {
            var state = (FileState)result.AsyncState;
            Stream stream = state.FileStream;
            stream.EndWrite(result);
            stream.Close();
        }
    }
}

