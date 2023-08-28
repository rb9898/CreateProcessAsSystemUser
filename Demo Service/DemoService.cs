using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using ProcessLauncher;

namespace Demo_Service
{
    public partial class DemoService : ServiceBase
    {
        public DemoService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            LaunchHelper.StartProcessAsSystemUser(@"C:\Windows\System32\notepad.exe");
        }

        protected override void OnStop()
        {
        }
    }
}
