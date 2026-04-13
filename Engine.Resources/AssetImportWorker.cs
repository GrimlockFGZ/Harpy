public class AssetImportWorker{

 public static void HandleImport(AssetInfo asset) => asset.Type switch
{
    AssetType.Shader    => ImportShader(asset),
    AssetType.Texture   => ImportTexture(asset),
    AssetType.Model     => ImportModel(asset),
    AssetType.Animation => ImportAnimation(asset),
    AssetType.Script    => ImportScript(asset),
    AssetType.Material  => ImportMaterial(asset),
    _                   => Console.WriteLine("Unsupported type")
};
  
