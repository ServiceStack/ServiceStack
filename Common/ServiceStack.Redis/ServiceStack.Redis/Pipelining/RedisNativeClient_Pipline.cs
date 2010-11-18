using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    public partial class RedisNativeClient
    {
        public abstract class PipelineCommand
        {
            private byte[][] cmdWithBinaryArgs_;
            protected readonly RedisNativeClient client_;

            public PipelineCommand()
            {
            }
            public PipelineCommand(RedisNativeClient client)
            {
                client_ = client;
            }
            public void init(params byte[][] cmdWithBinaryArgs)
            {
                cmdWithBinaryArgs_ = cmdWithBinaryArgs;
            }
            public void execute()
            {
                if (cmdWithBinaryArgs_ == null)
                {
                    var throwEx = new Exception(string.Format("Attempt to execute uninitialized pipleine command"));
                    log.Error(throwEx.Message);
                    throw throwEx;
                }
                if (!client_.SendCommand(cmdWithBinaryArgs_))
                    throw client_.CreateConnectionError();
            }
            public abstract void expect();
        }
        public class ExpectCodeCommand : PipelineCommand
        {
            public string Code { get; set; }
            public ExpectCodeCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                Code = client_.ExpectCode();
            }
        }
        public class ExpectSuccessCommand : PipelineCommand
        {
            public ExpectSuccessCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                client_.ExpectSuccess();
            }
        }
        public class ExpectIntCommand : PipelineCommand
        {
            private int expectedInt;
            public ExpectIntCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                expectedInt = client_.ReadInt();
            }
            public int getInt()
            {
                return expectedInt;
            }
        }
        public class ExpectDoubleCommand : PipelineCommand
        {
            private double expectedDouble;
            public ExpectDoubleCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                expectedDouble = client_.parseDouble(client_.ReadData());
            }
            public double getDouble()
            {
                return expectedDouble;
            }
        }
        public class ExpectStringCommand : PipelineCommand
        {
            private string expectedString;
            public ExpectStringCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                var bytes =  client_.ReadData();
                expectedString = bytes.FromUtf8Bytes();
            }
            public string getString()
            {
                return expectedString;
            }
        }
        public class ExpectWordCommand : PipelineCommand
        {
            private readonly string word_;
            public ExpectWordCommand(RedisNativeClient client) : base(client)
            {
            }
            public ExpectWordCommand(RedisNativeClient client, string word)
                : base(client)
            {
                word_ = word;
            }
            public override void expect()
            {
                client_.ExpectWord(word_);
            }
        }
        public class ExpectDataCommand : PipelineCommand
        {
            private byte[] data;
            public ExpectDataCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                data = client_.ReadData();
            }
            public byte[] getData()
            {
                return data;
            }
            
        }
        public class ExpectMultiDataCommand : PipelineCommand
        {
            private byte[][] multiData;
            public ExpectMultiDataCommand(RedisNativeClient client) : base(client)
            {
            }
            public override void expect()
            {
                multiData = client_.ReadMultiData();
            }
            public byte[][] getMultiData()
            {
                return multiData;
            }
        }
    }
}
