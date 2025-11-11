using Microsoft.AspNetCore.Mvc;

namespace InertiaCore.Utils;

public interface IInertiaSerializer
{
    public string Serialize(object? obj);

    public JsonResult SerializeResult(object? obj);
}
