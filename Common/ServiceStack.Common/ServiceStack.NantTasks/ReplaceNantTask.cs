using System;
using System.Collections.Generic;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;

namespace ServiceStack.NantTasks
{
    public abstract class ReplaceNantTask : NAnt.Core.Task
    {
        private string pattern = "*.*";

        [TaskAttribute("pattern")]
        public string Pattern
        {
            get { return pattern; }
            set { pattern = value; }
        }

        [TaskAttribute("baseDir", Required = true)]
        public string BaseDir { get; set; }

        [TaskAttribute("oldValue", Required = false)]
        public string OldValue { get; set; }

        [TaskAttribute("newValue", Required = false)]
        public string NewValue { get; set; }

        [TaskAttribute("oldValues", Required = false)]
        public string OldValues { get; set; }

        [TaskAttribute("newValues", Required = false)]
        public string NewValues { get; set; }

        protected string[] OldValuesArray { get; set; }
        protected string[] NewValuesArray { get; set; }

        protected override void ExecuteTask()
        {
            this.OldValuesArray = (OldValue != null) ? new[] { OldValue } : OldValues.Split(',');
            this.NewValuesArray = (NewValue != null) ? new[] { NewValue } : NewValues.Split(',');
            if (OldValuesArray.Length != NewValuesArray.Length)
            {
                throw new ArgumentException("OldValues.Length != NewValues.Length");
            }

            var dirInfo = new DirectoryInfo(this.BaseDir);
            WalkDirectory(dirInfo);
        }

        protected abstract void OnEachDirectory(DirectoryInfo dirInfo
            ,IList<string> oldValues, IList<string> newValues);

        private void WalkDirectory(DirectoryInfo dirInfo)
        {
            if (dirInfo == null) return;

            foreach (var dir in dirInfo.GetDirectories("*.*"))
            {
                WalkDirectory(dir);
            }

            OnEachDirectory(dirInfo, this.OldValuesArray, this.NewValuesArray);
        }

        public void Log(string message)
        {
            base.Log(Level.Info, message);
        }
    }
}