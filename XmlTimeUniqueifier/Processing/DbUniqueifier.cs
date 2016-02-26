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
using System.Linq;
using XmlTimeUniqueifier.DAL;
using XmlTimeUniqueifier.Model;

namespace XmlTimeUniqueifier.Processing
{
    /// <summary>
    /// Creates a unique event time by storing in the DB
    /// </summary>
    public class DbUniqueifier : IUniqueifier
    {
        // Database context
        EventsContext eventData = new EventsContext();

        private int history;
        private int eventCount;

        /// <summary>
        /// Creates the uniquifier
        /// </summary>
        /// <param name="historyLenght">Number of file to keep in the DB</param>
        public DbUniqueifier(int historyLenght)
        {
            history = historyLenght;
            eventCount = eventData.Events.Count();
        }

        /// <summary>
        /// Craete unique event date
        /// </summary>
        /// <param name="eventDate">Event date for record</param>
        /// <param name="patientCode">Patient code for the record</param>
        /// <returns>A unique event dat if one can be created</returns>
        public string Uniquify(string eventDate, string patientCode)
        {
            lock(this)
            {
                try
                {
                    List<Event> events = eventData.Events.Where(e => e.EventDate.StartsWith(eventDate) && e.PatientCode == patientCode).ToList();
                    for (int i = 0; i < 60; i++)
                    {
                        string updatedEventDate = string.Format("{0}:{1:D2}", eventDate, i);

                        if (events.FirstOrDefault(e => e.EventDate == updatedEventDate) == null)
                        {
                            Event newEvent = new Event() { EventDate = updatedEventDate, PatientCode = patientCode, Created = DateTime.UtcNow };
                            eventData.Events.Add(newEvent);
                            eventData.SaveChanges();
                            eventCount++;
                            return updatedEventDate;
                        }

                    }

                    throw new Exception("Unique file name cannot be created");
                }
                finally
                {
                    if (eventCount > history * 1.1)
                    {
                        Cleanup();
                    }
                }
            }
        }

        /// <summary>
        /// Cleanup history to avoid large data sets
        /// </summary>
        private void Cleanup()
        {
            // If history is lsee than one no cleanup is needed
            if(history < 1)
            {
                return;
            }

            IEnumerable<Event> eventsToRemove = eventData.Events.OrderByDescending(e => e.Created).Skip(history).ToList();
            eventData.Events.RemoveRange(eventsToRemove);
            eventCount = eventData.Events.Count();
        }
    }
}
