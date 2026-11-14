using PosCloud.Application.Common.Interfaces;
using PosCloud.Domain.Enums;

namespace PosCloud.Api.Background;

/// <summary>Fon vazifasi xatosini SuperAdmin "Xatoliklar"ga yozadi (kutmaydi, xato otmaydi).</summary>
internal static class BackgroundErrors
{
    public static void Report(IServiceScopeFactory scopeFactory, string task, Exception ex,
        SystemErrorSource source = SystemErrorSource.Background, SystemErrorLevel level = SystemErrorLevel.Error)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            scope.ServiceProvider.GetService<ISystemErrorRecorder>()
                ?.Record(SystemErrorEntry.FromException(source, ex, task, level));
        }
        catch
        {
            // yozib bo'lmadi — faqat log qoladi
        }
    }
}
