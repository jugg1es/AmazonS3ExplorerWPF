using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.ComponentModel;
using System.Collections;
using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.CloudFront.Model;
using Amazon.S3.Transfer;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Linq;

namespace AmazonS3ExplorerWPF
{

    public delegate void AmazonFileProgressChangeEventHandler(string remoteFilename, long currentBytes, long totalBytes, int percentDone);

    public class AmazonS3Service
    {
        private string _currentBucketName = string.Empty;
        public event AmazonFileProgressChangeEventHandler FileProgressChanged;
        private AmazonS3Client _client;
        private TransferUtility _transfer;
        private bool _cancelOperation = false;

        public string CurrentBucketName
        {
            get
            {
                return _currentBucketName;
            }
            set
            {
                _currentBucketName = value;
            }
        }

        public AmazonS3Service(Amazon.Runtime.AWSCredentials credentials)
        {
            _client = new AmazonS3Client(credentials);
            _transfer = new TransferUtility(_client);
        }
        public AmazonS3Service(string accessKeyID, string secret)
        {
            _client = new AmazonS3Client(accessKeyID, secret);
            _transfer = new TransferUtility(_client);
        }
        
        public void CancelOperation()
        {
            _cancelOperation = true;
        }

        public List<string> GetBuckets()
        {
            ListBucketsResponse resp = _client.ListBuckets();
            List<string> buckets = new List<string>();
            foreach(S3Bucket bucket in resp.Buckets)
            {
                buckets.Add(bucket.BucketName);
            }
            return buckets;
        }

        public List<S3Object> GetObjectsInBucket(string path)
        {
            List<S3Object> items = new List<S3Object>();


            var request = new ListObjectsRequest();
            ListObjectsRequest req = new ListObjectsRequest();
            req.BucketName = CurrentBucketName;
            req.Prefix = path;

            do
            {
                ListObjectsResponse resp = _transfer.S3Client.ListObjects(req);
                if (resp.IsTruncated)
                    req.Marker = resp.NextMarker;
                else req = null;
                items.AddRange(resp.S3Objects);
                if (_cancelOperation)
                {
                    _cancelOperation = false;
                    break;
                }
            }
            while (req != null);


            return items;
        }
        public void CreateFolder(string remotePath)
        {
            PutObjectRequest req = new PutObjectRequest();
            req.BucketName = CurrentBucketName;
            if (remotePath[remotePath.Length - 1] != '/')
                remotePath += "/";
            req.Key = remotePath;
            req.ContentBody = string.Empty;
            PutObjectResponse resp = _transfer.S3Client.PutObject(req);
        }
        public List<S3Object> GetObjectsInPath(string path)
        {
            List<S3Object> items = new List<S3Object>();


            var request = new ListObjectsRequest();
            ListObjectsRequest req = new ListObjectsRequest();
            req.BucketName = CurrentBucketName;
            req.Prefix = path;

            do
            {
                ListObjectsResponse resp = _transfer.S3Client.ListObjects(req);
                if (resp.IsTruncated)
                    req.Marker = resp.NextMarker;
                else req = null;

                items.AddRange(resp.S3Objects);
            }
            while (req != null);


            return items;
        }

        public byte[] DownloadFileByteContents(string remoteFilename)
        {
            byte[] buffer = null;

            TransferUtilityOpenStreamRequest req = new TransferUtilityOpenStreamRequest();
            req.BucketName = CurrentBucketName;
            req.Key = remoteFilename;

            using (Stream s = _transfer.OpenStream(req))
            {
                buffer = new byte[s.Length];
                s.Position = 0;
                s.Read(buffer, 0, (int)s.Length);
                s.Flush();
            }
            return buffer;
        }
      
        public string DownloadFileContents(string remoteFilename, string versionID = "")
        {
           
            string tempFile = System.IO.Path.GetTempFileName();

            TransferUtilityDownloadRequest req = new TransferUtilityDownloadRequest();
            req.BucketName = CurrentBucketName;
            req.FilePath = tempFile;
            if (string.IsNullOrEmpty(versionID) == false) req.VersionId = versionID;

            req.Key = remoteFilename;

            req.WriteObjectProgressEvent += (s, e) =>
            {
                if (FileProgressChanged != null)
                    FileProgressChanged(e.Key, e.TransferredBytes, e.TotalBytes, e.PercentDone);

            };
            _transfer.Download(req);

            string fileContents = File.ReadAllText(tempFile);
            File.Delete(tempFile);
            

            return fileContents;
        }

        public void DownloadFile(string remoteFilename, string localSavePath)
        {
            TransferUtilityDownloadRequest req = new TransferUtilityDownloadRequest();
            req.BucketName = CurrentBucketName;
            req.FilePath = localSavePath;

            req.Key = remoteFilename;
            req.WriteObjectProgressEvent += (s, e) =>
            {
                if (FileProgressChanged != null)
                    FileProgressChanged(e.Key, e.TransferredBytes, e.TotalBytes, e.PercentDone);

            };
            _transfer.Download(req);

        }
        public void UploadFile(string remoteFilename, string localFilename, Dictionary<string, string> metadata = null)
        {
            if (remoteFilename[0] == '/') remoteFilename = remoteFilename.Substring(1);
            TransferUtilityUploadRequest uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = CurrentBucketName,
                FilePath = localFilename,
                Key = remoteFilename,
                AutoCloseStream = true
            };
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    uploadRequest.Metadata.Add(kvp.Key, kvp.Value);
                }
            }
            uploadRequest.UploadProgressEvent += (sender, e) =>
            {
                if (FileProgressChanged != null)
                    FileProgressChanged(e.FilePath, e.TransferredBytes, e.TotalBytes, e.PercentDone);
            };

            _transfer.Upload(uploadRequest);
        }


        public void DeleteFile(string remotePath)
        {
            DeleteObjectRequest req = new DeleteObjectRequest();
            req.BucketName = CurrentBucketName;
            req.Key = remotePath;
            DeleteObjectResponse resp = _transfer.S3Client.DeleteObject(req);

        }
        public void DeleteFolder(string remotePath)
        {
            if (remotePath[remotePath.Length - 1] != '/') remotePath += "/";

            var request = new ListObjectsRequest();
            ListObjectsRequest req = new ListObjectsRequest();
            req.BucketName = CurrentBucketName;
            req.Prefix = remotePath;


            do
            {
                ListObjectsResponse resp = _transfer.S3Client.ListObjects(req);
                if (resp.IsTruncated)
                    req.Marker = resp.NextMarker;
                else
                    req = null;

                DeleteObjectsRequest deleteRequest = new DeleteObjectsRequest();
                deleteRequest.BucketName = CurrentBucketName;
                if (resp.S3Objects.Count == 0) break;
                foreach (var item in resp.S3Objects)
                {
                    deleteRequest.AddKey(item.Key);
                }
                DeleteObjectsResponse deleteResponse = _transfer.S3Client.DeleteObjects(deleteRequest);
            }
            while (req != null);

        }

        public void DeleteVersion(string remotePath, string versionID)
        {
            DeleteObjectRequest req = new DeleteObjectRequest();
            req.BucketName = CurrentBucketName;
            req.Key = remotePath;
            req.VersionId = versionID;
            DeleteObjectResponse resp = _transfer.S3Client.DeleteObject(req);

        }
        public void CopyFile(string sourceKey, string destinationKey)
        {
            CopyObjectRequest request = new CopyObjectRequest
            {
                SourceBucket = CurrentBucketName,
                SourceKey = sourceKey,
                DestinationBucket = CurrentBucketName,
                DestinationKey = destinationKey,
                StorageClass = Amazon.S3.S3StorageClass.Standard

            };
            CopyObjectResponse response = _transfer.S3Client.CopyObject(request);
        }
    }
}
