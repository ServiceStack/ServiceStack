﻿#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.AI;

public interface IPhrasesProvider
{
    /// <summary>
    /// Get Phrases to use
    /// </summary>
    Task<IEnumerable<string>> GetPhrasesAsync(CancellationToken token = default);
}