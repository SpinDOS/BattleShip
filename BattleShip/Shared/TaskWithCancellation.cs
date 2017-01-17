using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BattleShip.Shared
{
    public static class TaskWithCancellation
    {
        public static async Task<TResult> WithCancellation<TResult>(this Task<TResult> originalTask,
                CancellationToken ct)
        {
            // Создание объекта Task, завершаемого при отмене CancellationToken
            var cancelTask = new TaskCompletionSource<bool>();
            // При отмене CancellationToken завершить Task
            using (ct.Register(t => ((TaskCompletionSource<bool>) t).TrySetResult(true),cancelTask))
            {
                // Создание объекта Task, завершаемого при отмене исходного
                // объекта Task или объекта Task от CancellationToken
                Task any = await Task.WhenAny(originalTask, cancelTask.Task);
                // Если какой-либо объект Task завершается из-за CancellationToken,
                // инициировать OperationCanceledException
                if (any == cancelTask.Task) ct.ThrowIfCancellationRequested();
            }
            // Выполнить await для исходного задания (синхронно); 
            // если произойдет ошибка, выдать первое внутреннее исключение
            // вместо AggregateException
            return await originalTask;
        }

        public static async Task WithCancellation(this Task originalTask,
                CancellationToken ct)
        {
            // Создание объекта Task, завершаемого при отмене CancellationToken
            var cancelTask = new TaskCompletionSource<bool>();
            // При отмене CancellationToken завершить Task
            using (ct.Register(t => ((TaskCompletionSource<bool>) t).TrySetResult(true), cancelTask))
            {
                // Создание объекта Task, завершаемого при отмене исходного
                // объекта Task или объекта Task от CancellationToken
                Task any = await Task.WhenAny(originalTask, cancelTask.Task);
                // Если какой-либо объект Task завершается из-за CancellationToken,
                // инициировать OperationCanceledException
                if (any == cancelTask.Task) ct.ThrowIfCancellationRequested();
            }
            // Выполнить await для исходного задания (синхронно); 
            // если произойдет ошибка, выдать первое внутреннее исключение
            // вместо AggregateException
            await originalTask;
        }
    }
}
