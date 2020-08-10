﻿namespace ServiceStack
{
    public interface IRequiresSchema
    {
        /// <summary>
        /// Unified API to create any missing Tables, Data Structure Schema 
        /// or perform any other tasks dependencies require to run at Startup.
        /// </summary>
        void InitSchema();
    }
}