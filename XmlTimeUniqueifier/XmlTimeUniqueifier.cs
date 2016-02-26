/*
 * The MIT License(MIT)
 * Copyright(c) 2016
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 * documentation files (the "Software"), to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
 * WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using log4net;
using System;
using System.Configuration;
using System.ServiceProcess;
using XmlTimeUniqueifier.Processing;

namespace XmlTimeUniqueifier
{
    public partial class XmlTimeUniqueifier : ServiceBase
    {
        // Logger
        private static readonly ILog log = LogManager.GetLogger(typeof(XmlTimeUniqueifier));

        private FileMover fileMover;

        public XmlTimeUniqueifier()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            string sourceDirectory = ConfigurationManager.AppSettings["SourceDirectory"];
            string destinationDirectory = ConfigurationManager.AppSettings["DestinationDirectory"];
            string errorDirectory = ConfigurationManager.AppSettings["ErrorDirectory"];
            string updateInterval = ConfigurationManager.AppSettings["UpdateInterval"];
            string historyLengthValue = ConfigurationManager.AppSettings["HistoryLength"];
            string uniqueifierName = ConfigurationManager.AppSettings["Uniqueifier"];

            if (String.IsNullOrEmpty(sourceDirectory) || String.IsNullOrEmpty(destinationDirectory) || String.IsNullOrEmpty(errorDirectory))
            {
                log.Error("SourceDirectory, DestinationDirectory, and ErrorDirectory must be defined in the configuration file");
                throw new Exception("SourceDirectory, DestinationDirectory, and ErrorDirectory must be defined in the configuration file");
            }

            int interval;
            if(!int.TryParse(updateInterval, out interval) || interval < 1)
            {
                interval = 30;
                log.Info("UpdateInterval value not valid.  Change to default value of " + interval);
            }

            
            int historyLength;
            if (!int.TryParse(historyLengthValue, out historyLength))
            {
                interval = 1000;
                log.Info("HistoryLength value not valid.  Change to default value of " + historyLength);
            }

            IUniqueifier uniqueifier;
            if (String.IsNullOrEmpty(uniqueifierName) || String.IsNullOrEmpty(uniqueifierName) ||
                    string.Equals(uniqueifierName, "DB", StringComparison.OrdinalIgnoreCase))
            {
                uniqueifier = new DbUniqueifier(historyLength);
            }
            else if(string.Equals(uniqueifierName, "Memory", StringComparison.OrdinalIgnoreCase))
            {
                
                uniqueifier = new MemoryUniqueifier(historyLength);
            } 
            else
            {
                uniqueifier = new DbUniqueifier(historyLength);
            }


            try
            {
                log.Info("Starting service with configuration: " + Environment.NewLine +
                    "     Source Directory:      " + sourceDirectory + Environment.NewLine +
                    "     Destination Directory: " + destinationDirectory + Environment.NewLine +
                    "     Error Directory:       " + errorDirectory + Environment.NewLine +
                    "     History Length:        " + historyLength + Environment.NewLine +
                    "     Update Interval:       " + interval);
                // Create a file mover and start processing
                fileMover = new FileMover(sourceDirectory, destinationDirectory, errorDirectory, interval, uniqueifier);
                fileMover.Start();
            }
            catch (Exception e)
            {
                log.Error("Error occured starting the service", e);
                throw;
            }

        }

        protected override void OnStop()
        {
            if (fileMover != null)
            {
                fileMover.Stop();
                fileMover = null;
            }
        }
    }
}
