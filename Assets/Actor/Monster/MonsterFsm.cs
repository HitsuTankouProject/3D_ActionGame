using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

public class MonsterFsm
{
    public Func<CancellationToken, UniTask> FsmStart;

    public HashSet<MonsterStage> canReturnStages = new();
    public Func<CancellationToken, UniTask<MonsterStage>> FsmUpdate;

    public Func<CancellationToken, UniTask> FsmEnd;

}
