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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

namespace XmlTimeUniqueifier.Processing
{
    public class FileMover
    {
        // Logger
        private static readonly ILog log = LogManager.GetLogger(typeof(FileMover));

        // Engine to make the events unique
        private IUniqueifier uniqueifier;

        // Configuration info
        private DirectoryInfo sourceDirecotry;
        private DirectoryInfo destinationDirecotry;
        private DirectoryInfo errorDirecotry;
        private int updateInterval;

        // FImer and lock for processing
        private Timer timer;
        private Object lockObject = new object();

        /// <summary>
        /// Creates the file mover
        /// </summary>
        /// <param name="source">Directory to read from</param>
        /// <param name="destination">Directory to write to</param>
        /// /// <param name="error">Directory to error files to</param>
        /// <param name="updateInterval">Interval to check for new files</param>
        /// <param name="uniqueifier">Engine to make files unique</param>
        public FileMover(string source, string destination, string error, int updateInterval, IUniqueifier uniqueifier)
        {
            // Validate the source directory is valid and exists
            sourceDirecotry = new DirectoryInfo(source);
            if (!sourceDirecotry.Exists)
            {
                throw new ArgumentException("source parameter must refer to an existing directory", "source");
            }

            // Validate the destination directory is valid and exists
            if (!destination.EndsWith("\\"))
            {
                destination += "\\";
            }
            destinationDirecotry = new DirectoryInfo(destination);
            if (!destinationDirecotry.Exists)
            {
                throw new ArgumentException("destination parameter must refer to an existing directory", "destination");
            }

            // Validate the error directory is valid and exists
            if (!error.EndsWith("\\"))
            {
                error += "\\";
            }
            errorDirecotry = new DirectoryInfo(error);
            if (!errorDirecotry.Exists)
            {
                throw new ArgumentException("error parameter must refer to an existing directory", "error");
            }

            // Set the update interval
            this.updateInterval = updateInterval;

            // Create the uniqueifier engine
            this.uniqueifier = uniqueifier;

            // Create the update timer
            timer = new Timer((state) => { ProcessFiles(); });
        }

        /// <summary>
        /// Starts moving files
        /// </summary>
        public void Start()
        {
            // Start the timer
            timer.Change(0, updateInterval);
        }

        /// <summary>
        /// Stops moving files
        /// </summary>
        public void Stop()
        {
            // Stop the timer
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Process all files in the source directory
        /// </summary>
        private void ProcessFiles()
        {
            // Only allow one thread in here at a time
            if (Monitor.TryEnter(lockObject, 0))
            {
                try
                {
                    IEnumerable<String> allFiles = Directory.GetFiles(sourceDirecotry.FullName, "*", SearchOption.AllDirectories);
                    foreach (String fileName in allFiles)
                    {
                        FileInfo fileToProcess = null;
                        try
                        {
                            fileToProcess = new FileInfo(fileName);
                            ProcessFile(fileToProcess);
                        }
                        catch (Exception e)
                        {
                            // TODO Move file to error
                            log.Error(e);
                            if (fileToProcess != null)
                            {
                                log.Debug("Moving file to err: " + fileToProcess.Name);
                                try
                                {
                                    // Move the file to the error folder - add UtcNow.Ticks to make the file name unique
                                    File.Move(fileToProcess.FullName, errorDirecotry.FullName + fileToProcess.Name + "." + DateTime.UtcNow.Ticks);
                                }
                                catch (Exception error)
                                {
                                    log.Error(error);
                                }
                            }

                        }
                    }
                }
                finally
                {
                    Monitor.Exit(lockObject);
                }
            }
        }

        /// <summary>
        /// Process a file by updating it if it is XML or just moving it otherwise
        /// </summary>
        /// <param name="file">File to process</param>
        private void ProcessFile(FileInfo file)
        {
            if (!file.Exists || IsFileLocked(file))
            {
                // Ignore file if it is locked or doen't exist - next pass can pick it up
                return;
            }

            // Throw error of the file is already in the destination
            FileInfo destinationFile = new FileInfo(destinationDirecotry.FullName + file.Name);
            if (destinationFile.Exists)
            {
                throw new Exception("File already exists in the destination: " + destinationDirecotry.FullName + file.Name);
            }


            if (string.Compare(file.Extension, ".xml", true) == 0)
            {
                // Process XML
                log.Debug("Processing file: " + file.Name);

                string fileName = file.Name;
                XmlNode eventDate;
                XmlNode patientCode;

                try
                {
                    // Get the XML file in memory
                    XmlDocument doc = new XmlDocument();
                    doc.Load(file.FullName);

                    // Find the Patient code and event date
                    if (TryGetAttribute(doc, "Patient", "PatientCode", out patientCode) &&
                            TryGetAttribute(doc, "Event", "EventDate", out eventDate))
                    {
                        // If the file already has seconds, don't process it
                        if (eventDate.Value.Count(c => c == ':') > 1)
                        {
                            throw new Exception("XML file already has seconds: " + file.FullName);
                        }

                        // Update the event date to be unique
                        String eventDateValue = GetEventDateValue(patientCode.Value, eventDate.Value, fileName);

                        // Set the value in the XML file
                        eventDate.Value = eventDateValue;

                        // Save it to the destination
                        doc.Save(destinationFile.FullName);

                        // Delete the source document
                        file.Delete();
                    }
                    else
                    {
                        throw new Exception("XML file could not be parsed: " + file.FullName);
                    }
                }
                catch (Exception e)
                {
                    // IF there is any error in processing, just move the unmodified file
                    File.Move(file.FullName, destinationFile.FullName);
                    log.Debug("XML file " + file.Name + " cannot be processed or made unique.  Moving to destination without modification", e);
                }

            }
            else
            {
                // If it is not an XML file, move it into the destination directory
                File.Move(file.FullName, destinationFile.FullName);
                log.Debug("Moving file: " + file.Name);
            }
        }

        /// <summary>
        /// Gets a unique value for the event date by adding scronds
        /// </summary>
        /// <param name="patientCode">Patient code for the event</param>
        /// <param name="eventDate">Event date for the event</param>
        /// <param name="fileName">File name the event was read from</param>
        /// <returns>Unique event date value</returns>
        private string GetEventDateValue(string patientCode, string eventDate, string fileName)
        {
            return uniqueifier.Uniquify(eventDate, patientCode);
        }

        /// <summary>
        /// Reads an attribute from a XML tag
        /// </summary>
        /// <param name="doc">XML document to read from</param>
        /// <param name="nodeName">Name of tag that has the arrribute</param>
        /// <param name="attributeName">Name of attribute to return</param>
        /// <param name="returnNode">Attribute requested, or null if it is not not found</param>
        /// <returns>True if the attribute is found, otherwise false</returns>
        private static bool TryGetAttribute(XmlDocument doc, string nodeName, string attributeName, out XmlNode returnNode)
        {
            returnNode = null;

            XmlNodeList nodeList = doc.GetElementsByTagName(nodeName);
            if (nodeList == null || nodeList.Count < 1)
            {
                return false;
            }

            XmlNode node = nodeList.Item(0);
            if (node == null || node.Attributes == null)
            {
                return false;
            }

            XmlNode attributeNode = node.Attributes.GetNamedItem(attributeName);
            if (attributeNode == null || String.IsNullOrEmpty(attributeNode.Value))
            {
                return false;
            }

            returnNode = attributeNode;
            return true;
        }

        /// <summary>
        /// Checks if a file is locked
        /// </summary>
        /// <param name="file">File to check if it is locked</param>
        /// <returns>True if the file is locked</returns>
        private static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                // If opening the file throws an exception, it is locked
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return false;
        }
    }
}
