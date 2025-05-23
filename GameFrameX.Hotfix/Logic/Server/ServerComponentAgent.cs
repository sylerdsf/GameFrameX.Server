﻿using GameFrameX.Apps.Server.Component;
using GameFrameX.Apps.Server.Entity;
using GameFrameX.Core.Abstractions.Attribute;
using GameFrameX.Core.Abstractions.Events;
using GameFrameX.Core.Timer.Handler;
using GameFrameX.Hotfix.Logic.Player.Login;
using GameFrameX.Hotfix.Logic.Role.Login;

namespace GameFrameX.Hotfix.Logic.Server;

public class ServerComponentAgent : StateComponentAgent<ServerComponent, ServerState>
{
    public override async Task Active()
    {
        // 跨天定时器
        WithCronExpression<CrossDayTimeHandler>("0 0 0 * * ? *");
        if (State.FirstStartTime == default)
        {
            State.FirstStartTime = TimeHelper.UnixTimeSeconds();
            await OwnerComponent.WriteStateAsync();
        }
    }

    [Service]
    [Discard]
    public virtual ValueTask AddOnlineRole(long roleId)
    {
        OwnerComponent.OnlineSet.Add(roleId);
        return ValueTask.CompletedTask;
    }

    [Service]
    [Discard]
    public virtual ValueTask RemoveOnlineRole(long roleId)
    {
        OwnerComponent.OnlineSet.Remove(roleId);
        return ValueTask.CompletedTask;
    }

    public static async Task OnlineRoleForeach(Action<PlayerComponentAgent> func)
    {
        var serverComp = await ActorManager.GetComponentAgent<ServerComponentAgent>();
        serverComp.Tell(async () =>
        {
            foreach (var roleId in serverComp.OwnerComponent.OnlineSet)
            {
                var roleComp = await ActorManager.GetComponentAgent<PlayerComponentAgent>(roleId);
                roleComp.Tell(() => func(roleComp));
            }
        });
    }


    private Task TestDelayTimer()
    {
        LogHelper.Debug("ServerCompAgent.TestDelayTimer.延时3秒执行.执行一次");
        return Task.CompletedTask;
    }

    private Task TestScheduleTimer()
    {
        LogHelper.Debug("ServerCompAgent.TestSchedueTimer.延时1秒执行.每隔10秒执行");
        //
        // var states = await GameDb.FindListAsync<LoginState>(m => m.Id != 0);
        // LOGGER.Debug(states);
        //
        // var s1 = await GameDb.CountAsync<LoginState>(m => m.Id == 563517512475926528);
        // LOGGER.Debug(s1);
        // var s = await GameDb.FindAsync<LoginState>(m => m.UserName != null);
        // LOGGER.Debug(s);
        // LoginState loginState = new LoginState()
        // {
        //     Id = TimeHelper.UnixTimeSeconds(), CreateId = TimeHelper.UnixTimeSeconds(),
        //     UserName = "Save"
        // };
        // loginState.AfterLoadFromDB(true);
        // var savedState = await GameDb.UpdateAsync<LoginState>(loginState);
        // LOGGER.Debug(savedState);
        // await Task.Delay(TimeSpan.FromSeconds(1));
        // var count = await GameDb.DeleteAsync(savedState);
        // LOGGER.Debug(count);
        return Task.CompletedTask;
    }


    /// <summary>
    /// 获取服务器开服启动时间
    /// </summary>
    /// <returns></returns>
    [Service]
    [ThreadSafe]
    public virtual long FirstStartTime()
    {
        return State.FirstStartTime;
    }

    [Service]
    [ThreadSafe]
    public virtual Task<int> GetWorldLevel()
    {
        return Task.FromResult(State.WorldLevel);
    }

    [Service]
    public virtual Task<bool> IsOnline(long roleId)
    {
        foreach (var id in OwnerComponent.OnlineSet)
        {
            if (id == roleId)
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    /*******************演示代码**************************/
    [Service]
    [ThreadSafe]
    public virtual int DoSomething0()
    {
        return State.WorldLevel;
    }

    [Discard]
    [ThreadSafe]
    protected virtual Task DoSomething1()
    {
        return Task.CompletedTask;
    }

    [ThreadSafe]
    protected void DoSomething2()
    {
    }

    [Discard]
    [TimeOut(12000)]
    protected virtual Task DoSomething3()
    {
        return Task.CompletedTask;
    }

    private class DelayTimer : TimerHandler<ServerComponentAgent>
    {
        protected override Task HandleTimer(ServerComponentAgent agent, GameEventArgs gameEventArgs)
        {
            return agent.TestDelayTimer();
        }
    }

    private class ScheduleTimer : TimerHandler<ServerComponentAgent>
    {
        protected override Task HandleTimer(ServerComponentAgent agent, GameEventArgs gameEventArgs)
        {
            return agent.TestScheduleTimer();
        }
    }

    /// <summary>
    /// 跨天定时器调用
    /// </summary>
    private class CrossDayTimeHandler : TimerHandler<ServerComponentAgent>
    {
        protected override async Task HandleTimer(ServerComponentAgent agent, GameEventArgs gameEventArgs)
        {
            LogHelper.Debug($"ServerCompAgent.CrossDayTimeHandler.跨天定时器执行{TimeHelper.CurrentDateTimeWithUtcFullString()}");
            await ActorManager.RoleCrossDay(1);
            await ActorManager.CrossDay(1, GlobalConst.ActorTypeServer);
        }
    }
    /*******************演示代码**************************/
}