using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Examples.Tasks {
    [TaskName("usertask")]
    public class TestTask : Task {
        #region Private Instance Fields

        private string _message;

        #endregion Private Instance Fields

        #region Public Instance Properties

        [TaskAttribute("message", Required=true)]
        public string FileName {
            get { return _message; }
            set { _message = value; }
        }

        #endregion Public Instance Properties

        #region Override implementation of Task

        protected override void ExecuteTask() {
            Log(Level.Info, _message.ToUpper());
        }

        #endregion Override implementation of Task
    }
}
