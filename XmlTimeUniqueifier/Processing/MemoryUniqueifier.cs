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

using System;
using System.Collections.Generic;

namespace XmlTimeUniqueifier.Processing
{
    /// <summary>
    /// Creates a unique event time by storing history in memory
    /// </summary>
    public class MemoryUniqueifier : IUniqueifier
    {
        private int history;

        // Data structure to maintain state
        private HashSet<Entry> entrySet = new HashSet<Entry>();
        private Queue<Entry> entryQueue = new Queue<Entry>();

        /// <summary>
        /// Creates the uniquifier
        /// </summary>
        /// <param name="historyLenght">Number of file to keep in memory</param>
        public MemoryUniqueifier(int historyLenght)
        {
            history = historyLenght;
        }

        /// <summary>
        /// Craete unique event date
        /// </summary>
        /// <param name="eventDate">Event date for record</param>
        /// <param name="patientCode">Patient code for the record</param>
        /// <returns>A unique event dat if one can be created</returns>
        public String Uniquify(String eventDate, String patientCode)
        {
            // Keep data structures in sync
            lock (this)
            {
                try
                {
                    // Try all valid times until one works
                    for (int i = 0; i < 60; i++)
                    {
                        string updatedEventDate = string.Format("{0}:{1:D2}", eventDate, i);
                        Entry entry = new Entry(updatedEventDate, patientCode);
                        if (!entrySet.Contains(entry))
                        {
                            entrySet.Add(entry);
                            entryQueue.Enqueue(entry);
                            return entry.EventDate;
                        }
                    }
                }
                finally
                {
                    // When the history has hit 110% of its length, clean up extras
                    if (history > 1 && entryQueue.Count > history * 1.1)
                    {
                        while (entryQueue.Count > history)
                        {
                            entrySet.Remove(entryQueue.Dequeue());
                        }
                    }
                }
            }

            // If one cannot be create, throw an exception
            throw new Exception("Unique file name cannot be created");
        }

        /// <summary>
        /// Entry to be use in the collections
        /// </summary>
        private class Entry
        {
            /// <summary>
            /// Event date 
            /// </summary>
            public string EventDate { get; protected set; }
            /// <summary>
            /// Patient code
            /// </summary>
            public string PatientCode { get; protected set; }

            /// <summary>
            /// Creates the entry
            /// </summary>
            /// <param name="eventData">Event date for entry</param>
            /// <param name="patientCode">Patient code for entry</param>
            public Entry(string eventData, string patientCode)
            {
                EventDate = eventData;
                PatientCode = patientCode;
            }

            /// <summary>
            /// Craetes a hash code for fast lookup
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return (EventDate + PatientCode).GetHashCode();
            }

            /// <summary>
            /// Compares two entries for equivilance
            /// </summary>
            /// <param name="obj">Object to campare with</param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                Entry entry = obj as Entry;
                if (entry == null)
                {
                    return false;
                }

                return entry.EventDate == EventDate && entry.PatientCode == PatientCode;
            }
        }
    }
}
