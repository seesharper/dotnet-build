#r "nuget:Octokit, 0.27.0"
#load "FileUtils.csx"
using Octokit;
using static FileUtils;
public static class ReleaseManagement
{
    public static ReleaseManager ReleaseManagerFor(string owner, string repository, string accessToken)
    {
        return new ReleaseManager(owner, repository, accessToken);
    }

    public class ReleaseManager
    {
        private readonly string _owner;
        private readonly string _repository;
        private readonly string _accessToken;

        internal ReleaseManager(string owner, string repository, string accessToken)
        {
            this._owner = owner;
            this._repository = repository;
            this._accessToken = accessToken;
        }




        public async Task CreateRelease(string tag, string pathToReleaseNotes, ReleaseAsset[] releaseAssets)
        {
            var client = new GitHubClient(new ProductHeaderValue(_repository));
            var tokenAuth = new Credentials(_accessToken);
            client.Credentials = tokenAuth;




            var releaseNotes = ReadFile(pathToReleaseNotes);
            var allReleases = await client.Repository.Release.GetAll(_owner, _repository);

            var existingRelease = allReleases.SingleOrDefault(r => r.TagName == tag);

            if (existingRelease != null)
            {
                var releaseUpdate = new ReleaseUpdate();
                releaseUpdate.Body = releaseNotes;
                releaseUpdate.Name = tag;

                await client.Repository.Release.Edit(_owner, _repository, existingRelease.Id, releaseUpdate);
                await UploadReleaseAssets(releaseAssets, client, existingRelease);
            }
            else
            {
                var newRelease = new NewRelease(tag);
                newRelease.Name = tag;
                newRelease.Body = releaseNotes;
                newRelease.Draft = false;
                newRelease.Prerelease = tag.Contains("-");

                var createdRelease = client.Repository.Release.Create(_owner, _repository, newRelease).Result;

                await UploadReleaseAssets(releaseAssets, client, createdRelease);
            }
        }

        private static async Task UploadReleaseAssets(ReleaseAsset[] releaseAssets, GitHubClient client, Release release)
        {
            foreach (var releaseAsset in releaseAssets)
            {
                var archiveContents = File.OpenRead(releaseAsset.Path);
                var assetUpload = new ReleaseAssetUpload()
                {
                    FileName = Path.GetFileName(releaseAsset.Path),
                    ContentType = releaseAsset.ContentType,
                    RawData = archiveContents
                };
                await client.Repository.Release.UploadAsset(release, assetUpload);
            }
        }
    }

    public abstract class ReleaseAsset
    {
        public ReleaseAsset(string path)
        {
            Path = path;
        }

        public string Path { get; }
        public abstract string ContentType { get; }
    }

    public class ZipReleaseAsset : ReleaseAsset
    {
        public ZipReleaseAsset(string path) : base(path)
        {
        }

        public override string ContentType => "application/zip";
    }

    public class OctetStreamReleaseAsset : ReleaseAsset
    {
        public OctetStreamReleaseAsset(string path) : base(path)
        {
        }

        public override string ContentType => "application/octet-stream";
    }


}