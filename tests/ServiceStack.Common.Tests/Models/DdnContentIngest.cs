using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Common;
using ServiceStack.Text;

namespace ServiceStack.Common.Tests.Models
{
    public interface IExternal
    {
        string ExternalUrn { get; set; }
    }

    public interface IExternalDeletable : IExternal
    {
        bool Delete { get; set; }
    }

    public interface IContent : IExternal
    {
        Guid Id { get; set; }
        string Urn { get; set; }

        DateTime CreatedDate { get; set; }
        DateTime ModifiedDate { get; set; }
    }

    public interface IContentDeletable : IContent
    {
        DateTime? DeletedDate { get; set; }
    }

    public class MergeList<T>
    {
        public bool Partial { get; set; }
        public List<T> Items { get; set; }

        public MergeList()
        {
        }

        public MergeList(IEnumerable<T> items)
        {
            this.Items = new List<T>(items);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}s", this.Partial ? "Partial" : "Full", this.Items.NullableCount(), typeof(T).Name);
        }
    }

    public class CostPoint
    {
        public string Campaign { get; set; }
        public DateTime StartDate { get; set; }
        public string CostCode { get; set; }
    }

    public enum ExplicitType
    {
        Unknown,
        NotExplicit,
        Explicit,
        Cleaned
    }

    public enum ReleaseType
    {
        Single,
        Album,
        Ep,
        BoxedSet
    }

    public class Batch
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string SupplierKeyName { get; set; }
        public string SupplierName { get; set; }
        public string DeliveryKeyName { get; set; }
        public long SequenceNumber { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.SupplierKeyName, this.Name);
        }
    }

    public class Participant
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.Role);
        }
    }

    public class ArtistUpdate : IExternal
    {
        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public DateTime? BirthDate { get; set; }
        public string BirthPlace { get; set; }

        public DateTime? DeathDate { get; set; }
        public string DeathPlace { get; set; }

        public string DecadesActive { get; set; }

        public string Biography { get; set; }
        public string BiographyAuthor { get; set; }

        public List<AssetId> AssetIds { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.ExternalRef);
        }
    }

    public class AssetUpdate : IExternal
    {
        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }

        public AssetType AssetType { get; set; }

        public string ExternalOwnerUrn { get; set; }

        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public long FileSizeBytes { get; set; }

        public string MasterSha256Checksum { get; set; }
        public string Md5Checksum { get; set; }

        public int? DurationMs { get; set; }
        public int? BitRateKbps { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public string TranscodedAssetPath { get; set; }

        public string BuildExternalRef()
        {
            switch (this.AssetType)
            {
                case AssetType.MasterCoverArt:
                case AssetType.MasterArtistArt:
                case AssetType.MasterLabelArt:
                    {
                        return String.Format("{0}/{1}", this.AssetType, this.MasterSha256Checksum).ToLower();
                    }
                case AssetType.TrackProduct:
                case AssetType.TrackPreview:
                    {
                        var externalOwnerRef = Urn.Parse(this.ExternalOwnerUrn).IdValue;
                        return String.Format("{0}/{1}/{2}/{3}/{4}/{5}", this.AssetType, this.MasterSha256Checksum, externalOwnerRef, this.FileExtension.ToLower(), this.BitRateKbps ?? 0, this.DurationMs ?? 0).ToLower();
                    }
                default:
                    {
                        var message = String.Format("AssetType not supported: {0}-{1}-{2}", AssetType, FileExtension.ToLower(), MasterSha256Checksum);
                        throw new Exception(message);
                    }
            }
        }

        public AssetId CreateAssetId()
        {
            return new AssetId { AssetType = this.AssetType, ExternalUrn = this.ExternalUrn };
        }

        public override string ToString()
        {
            return this.ExternalRef;
        }
    }

    public class LabelUpdate : IExternal
    {
        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public DateTime? EstablishedDate { get; set; }

        public string Biography { get; set; }
        public string BiographyAuthor { get; set; }

        public List<AssetId> AssetIds { get; set; }

        public List<AssetId> GetImageAssetIds()
        {
            return AssetIds.Where(x => x.AssetType.IsImage()).ToList();
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.ExternalRef);
        }
    }

    public class ProductUpdate
    {
        public bool Delete { get; set; }

        public string TerritoryCode { get; set; }
        public string Copyright { get; set; }
        public string ReportRef { get; set; }

        public bool AllowDownload { get; set; }
        public bool AllowStreaming { get; set; }
        public bool AllowSubscription { get; set; }

        public bool CollectionOnly { get; set; }

        public DateTime? DownloadStartDate { get; set; }
        public DateTime? DownloadEndDate { get; set; }

        public List<CostPoint> CostPoints { get; set; }

        public DateTime? StreamingStartDate { get; set; }
        public DateTime? StreamingEndDate { get; set; }

        public override string ToString()
        {
            return this.TerritoryCode;
        }
    }

    public class ReleaseChangeSet
    {
        public Guid BatchId { get; set; }
        public string BatchName { get; set; }
        public long BatchSequence { get; set; }
        public string BatchPath { get; set; }

        public bool Partial { get; set; }

        public ReleaseUpdate Release { get; set; }

        public List<LabelUpdate> Labels { get; set; }
        public List<ArtistUpdate> Artists { get; set; }
        public List<TrackUpdate> Tracks { get; set; }
        public List<AssetUpdate> Assets { get; set; }

        public void UpdateBatchInfo(Batch batch)
        {
            this.BatchId = batch.Id;
            this.BatchName = batch.Name;
            this.BatchSequence = batch.SequenceNumber;
            this.BatchPath = batch.Path;
        }

        public override string ToString()
        {
            return String.Format("{0} {1} {2}", this.Release.ExternalUrn, this.BatchName, this.Partial ? "Partial" : "Full");
        }
    }

    public static class ReleaseChangeSetSerializer
    {
        public static string Serialize(ReleaseChangeSet changeSet)
        {
            return TypeSerializer.SerializeToString(changeSet);
        }

        public static ReleaseChangeSet Deserialize(string changeSetString)
        {
            return TypeSerializer.DeserializeFromString<ReleaseChangeSet>(changeSetString);
        }
    }

    public class TrackUpdate : IExternalDeletable
    {
        public bool Delete { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public string NameExVersion { get; set; }
        public string NameVersion { get; set; }

        public string LabelText { get; set; }
        public string ArtistText { get; set; }
        public string ReleaseText { get; set; }

        public string Isrc { get; set; }
        public string GlobalReleaseId { get; set; }

        public int? SetNumber { get; set; }
        public int? DiscNumber { get; set; }
        public int? TrackNumber { get; set; }
        public int? SequenceNumber { get; set; }

        public int? DurationMs { get; set; }

        public ExplicitType ExplicitType { get; set; }

        public string RightsHolder { get; set; }
        public string Copyright { get; set; }

        public List<string> Publishers { get; set; }

        public List<string> Genres { get; set; }
        public List<string> SubGenres { get; set; }

        public string Review { get; set; }
        public string ReviewAuthor { get; set; }

        public string Lyrics { get; set; }

        public List<Participant> Participants { get; set; }

        public string ExternalLabelUrn { get; set; }
        public string ExternalReleaseUrn { get; set; }

        public MergeList<string> ExternalArtistUrns { get; set; }
        public MergeList<ProductUpdate> Products { get; set; }

        public List<AssetId> AssetIds { get; set; }

        public void AddAssetIds(IEnumerable<AssetId> assetIds)
        {
            if (assetIds == null || !assetIds.Any())
            {
                return;
            }

            if (this.AssetIds == null)
            {
                this.AssetIds = new List<AssetId>();
            }

            this.AssetIds.AddRange(assetIds);
        }

        public List<AssetId> GetImageAssetIds()
        {
            return this.AssetIds.SafeWhere(x => x.AssetType.IsImage()).ToList();
        }

        public List<AssetId> GetAudioAssetIds()
        {
            return this.AssetIds.SafeWhere(x => x.AssetType.IsAudio()).ToList();
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.ExternalRef);
        }
    }

    public class ReleaseUpdate : IExternalDeletable
    {
        public const string WorldTerritoryCode = "ZZ";

        public bool Delete { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public string NameExVersion { get; set; }
        public string NameVersion { get; set; }

        public ReleaseType ReleaseType { get; set; }

        public string LabelText { get; set; }
        public string ArtistText { get; set; }

        public string UpcEan { get; set; }
        public string GlobalReleaseId { get; set; }
        public string CatalogueNumber { get; set; }

        public int? SetCount { get; set; }
        public int? DiscCount { get; set; }
        public int? TrackCount { get; set; }

        public int? DurationMs { get; set; }
        public bool ContinuousMix { get; set; }

        public ExplicitType ExplicitType { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public string RightsHolder { get; set; }
        public string Copyright { get; set; }

        public List<string> Genres { get; set; }
        public List<string> SubGenres { get; set; }

        public string Review { get; set; }
        public string ReviewAuthor { get; set; }

        public List<Participant> Participants { get; set; }

        public string ExternalLabelUrn { get; set; }

        public MergeList<string> ExternalArtistUrns { get; set; }
        public MergeList<string> ExternalTrackUrns { get; set; }
        public MergeList<ProductUpdate> Products { get; set; }

        public List<AssetId> AssetIds { get; set; }

        public void AddAssetIds(IEnumerable<AssetId> assetIds)
        {
            if (assetIds == null || !assetIds.Any())
            {
                return;
            }

            if (this.AssetIds == null)
            {
                this.AssetIds = new List<AssetId>();
            }

            this.AssetIds.AddRange(assetIds);
        }

        public List<AssetId> GetImageAssetIds()
        {
            return this.AssetIds.SafeWhere(x => x.AssetType.IsImage()).ToList();
        }

        public List<AssetId> GetAudioAssetIds()
        {
            return this.AssetIds.SafeWhere(x => x.AssetType.IsAudio()).ToList();
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.ExternalRef);
        }
    }

    public class AssetId
    {
        public AssetType AssetType { get; set; }
        public string ExternalUrn { get; set; }

        public override string ToString()
        {
            return this.ExternalUrn;
        }
    }

    public class Artist : IContent
    {
        public Guid Id { get; set; }
        public string Urn { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public DateTime? BirthDate { get; set; }
        public string BirthPlace { get; set; }

        public DateTime? DeathDate { get; set; }
        public string DeathPlace { get; set; }

        public string DecadesActive { get; set; }

        public string Biography { get; set; }
        public string BiographyAuthor { get; set; }

        public List<Asset> Assets { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public List<Asset> GetImageAssets()
        {
            return this.Assets.Where(x => x.AssetType.IsImage()).ToList();
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.Id.ToString("N"));
        }
    }

    public class Asset : IContent
    {
        public Guid Id { get; set; }
        public string Urn { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }

        public AssetType AssetType { get; set; }

        public string ExternalOwnerUrn { get; set; }

        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public long FileSizeBytes { get; set; }

        public string MasterSha256Checksum { get; set; }
        public string Md5Checksum { get; set; }

        public int? DurationMs { get; set; }
        public int? BitRateKbps { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public string TranscodedAssetPath { get; set; }
        public string RepositoryAssetPath { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.ExternalRef, this.Id.ToString("N"));
        }
    }

    public class Label : IContent
    {
        public Guid Id { get; set; }
        public string Urn { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public DateTime? EstablishedDate { get; set; }

        public string Biography { get; set; }
        public string BiographyAuthor { get; set; }

        public List<Asset> Assets { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public List<Asset> GetImageAssets()
        {
            return this.Assets.Where(x => x.AssetType.IsImage()).ToList();
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.Id.ToString("N"));
        }
    }

    public class Product
    {
        public string TerritoryCode { get; set; }
        public string Copyright { get; set; }
        public string ReportRef { get; set; }

        public bool AllowDownload { get; set; }
        public bool AllowStreaming { get; set; }
        public bool AllowSubscription { get; set; }

        public bool CollectionOnly { get; set; }

        public DateTime? DownloadStartDate { get; set; }
        public DateTime? DownloadEndDate { get; set; }

        public List<CostPoint> CostPoints { get; set; }

        public DateTime? StreamingStartDate { get; set; }
        public DateTime? StreamingEndDate { get; set; }

        public override string ToString()
        {
            return this.TerritoryCode;
        }
    }


    public class Release : IContent
    {
        public const string WorldTerritoryCode = "ZZ";

        public Guid Id { get; set; }
        public string Urn { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public string NameExVersion { get; set; }
        public string NameVersion { get; set; }

        public ReleaseType ReleaseType { get; set; }

        public string LabelText { get; set; }
        public string ArtistText { get; set; }

        public string UpcEan { get; set; }
        public string GlobalReleaseId { get; set; }
        public string CatalogueNumber { get; set; }

        public int SetCount { get; set; }
        public int DiscCount { get; set; }
        public int TrackCount { get; set; }

        public int? DurationMs { get; set; }
        public bool ContinuousMix { get; set; }

        public ExplicitType ExplicitType { get; set; }

        public DateTime? ReleaseDate { get; set; }

        public string RightsHolder { get; set; }
        public string Copyright { get; set; }

        public List<string> Genres { get; set; }
        public List<string> SubGenres { get; set; }

        public string Review { get; set; }
        public string ReviewAuthor { get; set; }

        public List<Participant> Participants { get; set; }

        public Label Label { get; set; }
        public List<Artist> Artists { get; set; }
        public List<Track> Tracks { get; set; }
        public List<Product> Products { get; set; }
        public List<Asset> Assets { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }

        public string GetPrimaryGenre()
        {
            return this.Genres.IsNullOrEmpty() ? null : this.Genres[0];
        }

        public Artist GetPrimaryArtist()
        {
            return this.Artists.IsNullOrEmpty() ? null : this.Artists[0];
        }

        public List<Asset> GetImageAssets()
        {
            return this.Assets.Where(x => x.AssetType.IsImage()).ToList();
        }

        public List<Asset> GetAudioAssets()
        {
            return this.Assets.Where(x => x.AssetType.IsAudio()).ToList();
        }

        public List<Track> GetActiveTracks()
        {
            return this.Tracks.SafeWhere(x => !x.CollectionOrphan).ToList();
        }

        public List<Track> GetOrphanTracks()
        {
            return this.Tracks.SafeWhere(x => x.CollectionOrphan).ToList();
        }

        public Product GetProduct(string territoryCode)
        {
            if (this.Products.IsNullOrEmpty())
            {
                return null;
            }

            var product = this.Products.FirstOrDefault(x => x.TerritoryCode.EqualsIgnoreCase(territoryCode));
            if (product == null)
            {
                // Default to the world product if exists
                product = this.Products.FirstOrDefault(x => x.TerritoryCode.EqualsIgnoreCase(WorldTerritoryCode));
            }

            return product;
        }

        public string GetDescription()
        {
            return String.Format("{0} by {1}", this.Name, this.ArtistText);
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.Id.ToString("N"));
        }
    }

    public class Track : IContentDeletable
    {
        public Guid Id { get; set; }
        public string Urn { get; set; }

        public string ExternalRef { get; set; }
        public string ExternalUrn { get; set; }

        public string SupplierKeyName { get; set; }
        public string Name { get; set; }

        public string NameExVersion { get; set; }
        public string NameVersion { get; set; }

        public string LabelText { get; set; }
        public string ArtistText { get; set; }
        public string ReleaseText { get; set; }

        public string Isrc { get; set; }
        public string GlobalReleaseId { get; set; }

        public bool CollectionOrphan { get; set; }

        public int SetNumber { get; set; }
        public int DiscNumber { get; set; }
        public int TrackNumber { get; set; }
        public int SequenceNumber { get; set; }

        public int? DurationMs { get; set; }

        public ExplicitType ExplicitType { get; set; }

        public string RightsHolder { get; set; }
        public string Copyright { get; set; }

        public List<string> Publishers { get; set; }

        public List<string> Genres { get; set; }
        public List<string> SubGenres { get; set; }

        public string Review { get; set; }
        public string ReviewAuthor { get; set; }

        public string Lyrics { get; set; }

        public List<Participant> Participants { get; set; }

        public Label Label { get; set; }
        public Release Release { get; set; }
        public List<Artist> Artists { get; set; }
        public List<Product> Products { get; set; }
        public List<Asset> Assets { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }

        public string GetPrimaryGenre()
        {
            return this.Genres.IsNullOrEmpty() ? null : this.Genres[0];
        }

        public Artist GetPrimaryArtist()
        {
            return this.Artists.IsNullOrEmpty() ? null : this.Artists[0];
        }

        public List<Asset> GetImageAssetIds()
        {
            return this.Assets.Where(x => x.AssetType.IsImage()).ToList();
        }

        public List<Asset> GetAudioAssetIds()
        {
            return this.Assets.Where(x => x.AssetType.IsAudio()).ToList();
        }

        public Product GetProduct(string territoryCode)
        {
            if (this.Products.IsNullOrEmpty())
            {
                return null;
            }

            var product = this.Products.FirstOrDefault(x => x.TerritoryCode.EqualsIgnoreCase(territoryCode));
            if (product == null)
            {
                // Default to the world product if exists
                product = this.Products.FirstOrDefault(x => x.TerritoryCode.EqualsIgnoreCase(Release.WorldTerritoryCode));
            }

            return product;
        }

        public string GetDescription()
        {
            return String.Format("{0} {1} ({2}) by {3}", this.SequenceNumber, this.Name, this.ReleaseText, this.ArtistText);
        }

        public override string ToString()
        {
            return String.Format("{0} {1}", this.Name, this.Id.ToString("N"));
        }
    }

    public enum AssetType
    {
        None,
        MasterCoverArt,
        MasterArtistArt,
        MasterLabelArt,
        ResizedCoverArt,
        ResizedArtistArt,
        ResizedLabelArt,
        TrackProduct,
        TrackPreview,
    }

    public static class AssetTypeExtensions
    {
        public static bool IsAudio(this AssetType assetType)
        {
            return assetType == AssetType.TrackProduct || assetType == AssetType.TrackPreview;
        }

        public static bool IsMasterImage(this AssetType assetType)
        {
            return assetType == AssetType.MasterCoverArt || assetType == AssetType.MasterArtistArt || assetType == AssetType.MasterLabelArt;
        }

        public static bool IsResizedImage(this AssetType assetType)
        {
            return assetType == AssetType.ResizedCoverArt || assetType == AssetType.ResizedArtistArt || assetType == AssetType.ResizedLabelArt;
        }

        public static bool IsImage(this AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.MasterCoverArt:
                case AssetType.MasterArtistArt:
                case AssetType.MasterLabelArt:
                case AssetType.ResizedCoverArt:
                case AssetType.ResizedArtistArt:
                case AssetType.ResizedLabelArt:
                    return true;
                default:
                    return false;
            }
        }

        public static AssetType GetMasterType(this AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.ResizedCoverArt:
                    return AssetType.MasterCoverArt;
                case AssetType.ResizedArtistArt:
                    return AssetType.MasterArtistArt;
                case AssetType.ResizedLabelArt:
                    return AssetType.MasterLabelArt;
                default:
                    return assetType;
            }
        }

        public static AssetType GetResizedType(this AssetType assetType)
        {
            switch (assetType)
            {
                case AssetType.MasterCoverArt:
                    return AssetType.ResizedCoverArt;
                case AssetType.MasterArtistArt:
                    return AssetType.ResizedArtistArt;
                case AssetType.MasterLabelArt:
                    return AssetType.ResizedLabelArt;
                default:
                    return assetType;
            }
        }
    }

    public struct Urn
    {
        private const char IdValueSeperator = '/';
        private readonly string urnString;

        public string ResourceName
        {
            get;
            private set;
        }

        public string IdTypeName
        {
            get;
            private set;
        }

        public string IdValue
        {
            get;
            private set;
        }

        public string[] IdValues
        {
            get
            {
                return IdValue.Split(IdValueSeperator);
            }
        }

        public Urn(string resourceName, string idTypeName, string idValue)
            : this()
        {
            if (resourceName == null)
            {
                throw new ArgumentNullException("resourceName");
            }

            if (idValue == null)
            {
                throw new ArgumentNullException("idValue");
            }

            this.ResourceName = resourceName.ToLower();
            this.IdTypeName = !String.IsNullOrEmpty(idTypeName) ? idTypeName.ToLower() : null;
            this.IdValue = idValue;

            if (String.IsNullOrEmpty(this.IdTypeName))
            {
                this.urnString = string.Format("urn:{0}:{1}", this.ResourceName, this.IdValue);
            }
            else
            {
                this.urnString = string.Format("urn:{0}:{1}:{2}", this.ResourceName, this.IdTypeName, this.IdValue);
            }
        }

        public Urn(string resourceName, string idValue)
            : this(resourceName, null, idValue)
        {
        }

        public bool IsDefaultIdType()
        {
            return String.IsNullOrEmpty(IdTypeName);
        }

        public bool IsResourceType(string resourceName)
        {
            return string.Compare(this.ResourceName, resourceName, true) == 0;
        }

        public bool IsIdType(Type type)
        {
            return this.IsIdType(type.Name);
        }

        public bool IsIdType(string idTypeName)
        {
            return string.Compare(this.IdTypeName, idTypeName, true) == 0;
        }

        public override int GetHashCode()
        {
            return this.urnString.GetHashCode();
        }

        public bool Equals(Urn obj)
        {
            return String.CompareOrdinal(obj.urnString, this.urnString) == 0;
        }

        public override bool Equals(object obj)
        {
            return obj.GetType() == typeof(Urn) && Equals(obj);
        }

        public override string ToString()
        {
            return urnString;
        }

        // Operators overloading

        public static implicit operator string(Urn urn)
        {
            return urn.urnString;
        }

        public static bool operator ==(Urn urn1, Urn urn2)
        {
            return urn1.Equals(urn2);
        }

        public static bool operator !=(Urn urn1, Urn urn2)
        {
            return !urn1.Equals(urn2);
        }

        // Parsing

        public static bool IsValidUrn(string urnString)
        {
            var fields = urnString.Split(':');
            return (fields.Length == 3 || fields.Length == 4) && String.CompareOrdinal(fields[0], "urn") == 0;
        }

        public static bool TryParse(string urnString, out Urn urn)
        {
            var fields = urnString.Split(':');

            if ((fields.Length == 3 || fields.Length == 4) && String.CompareOrdinal(fields[0], "urn") == 0)
            {
                if (fields.Length == 4)
                {
                    urn = new Urn(fields[1], fields[2], fields[3]);
                }
                else
                {
                    urn = new Urn(fields[1], fields[2]);
                }

                return true;
            }

            urn = new Urn();
            return false;
        }

        public static Urn Parse(string urnText)
        {
            if (String.IsNullOrEmpty(urnText))
            {
                throw new ArgumentNullException("urnText");
            }

            var fields = urnText.Split(':');
            if ((fields.Length == 3 || fields.Length == 4) && String.CompareOrdinal(fields[0], "urn") == 0)
            {
                return fields.Length == 4 ? new Urn(fields[1], fields[2], fields[3]) : new Urn(fields[1], fields[2]);
            }

            var msg = string.Format("Invalid URN text '{0}'", urnText);
            throw new FormatException(msg);
        }

        public static string GetUrnType(string urnString)
        {
            var urn = Parse(urnString);
            return urn.ResourceName;
        }

        public static long GetLongId(string urnString)
        {
            var urn = Parse(urnString);
            return Convert.ToInt64(urn.IdValue);
        }

        public static Guid GetGuidId(string urnString)
        {
            var urn = Parse(urnString);
            return new Guid(urn.IdValue);
        }

        public static string[] GetIdValues(string urnString)
        {
            var urn = Parse(urnString);
            return urn.IdValues;
        }

        public static string GetIdValue(string urnString)
        {
            var urn = Parse(urnString);
            return urn.IdValue;
        }

        public static string CleanIdValue(string idValue)
        {
            return idValue.Trim().ToLowerInvariant().Replace(' ', '_');
        }

        public static string GetFirstIdValue(string urnString)
        {
            var urn = Parse(urnString);
            return urn.IdValue.Split('/')[0];
        }

        public static string GetSecondIdValue(string urnString)
        {
            var urn = Parse(urnString);
            var parts = urn.IdValue.Split('/');
            return parts.Length > 1 ? parts[1] : null;
        }
    }

}