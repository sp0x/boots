﻿using System.Collections.Generic;

namespace Netlyt.Interfaces.Cloud
{
    public interface xICloudNodeNotification
    {
        string Token { get; }
        Dictionary<string, string> Headers { get; }
    }
}