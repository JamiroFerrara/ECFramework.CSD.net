using System.Collections.Generic;

public static class MimeType
{
    public static class Image
    {
        public const string ImageJpeg = "image/jpeg";
        public const string ImagePng = "image/png";
        public const string ImageGif = "image/gif";
        public const string ImageBmp = "image/bmp";
        public const string ImageSvg = "image/svg+xml";
        public const string ImageWebp = "image/webp";
        public const string ImageTiff = "image/tiff";
        public const string ImageIco = "image/x-icon";
        public const string ImageHeic = "image/heic";
        public const string ImageHeif = "image/heif";
        public const string ImageAvif = "image/avif";

        public static bool IsImage(string mimeType)
        {
            return new List<string>
            {
                ImageJpeg,
                ImagePng,
                ImageGif,
                ImageBmp,
                ImageSvg,
                ImageWebp,
                ImageTiff,
                ImageIco,
                ImageHeic,
                ImageHeif,
                ImageAvif
            }.Contains(mimeType);
        }
    }

    public static class Audio
    {
        public const string AudioMpeg = "audio/mpeg";
        public const string AudioOgg = "audio/ogg";
        public const string AudioWav = "audio/wav";
        public const string AudioWebm = "audio/webm";
        public const string AudioAac = "audio/aac";
        public const string AudioFlac = "audio/flac";
        public const string AudioMidi = "audio/midi";
        public const string AudioXMidi = "audio/x-midi";
        public const string AudioOpus = "audio/opus";
        public const string Audio3gpp = "audio/3gpp";
        public const string Audio3gpp2 = "audio/3gpp2";

        public static bool IsAudio(string mimeType)
        {
            return new List<string>
            {
                AudioMpeg,
                AudioOgg,
                AudioWav,
                AudioWebm,
                AudioAac,
                AudioFlac,
                AudioMidi,
                AudioXMidi,
                AudioOpus,
                Audio3gpp,
                Audio3gpp2
            }.Contains(mimeType);
        }
    }

    public static class Video
    {
        public const string VideoMp4 = "video/mp4";
        public const string VideoMpeg = "video/mpeg";
        public const string VideoOgg = "video/ogg";
        public const string VideoWebm = "video/webm";
        public const string VideoAvi = "video/x-msvideo";
        public const string Video3gpp = "video/3gpp";
        public const string Video3gpp2 = "video/3gpp2";
        public const string VideoMov = "video/quicktime";
        public const string VideoFlv = "video/x-flv";
        public const string VideoMkv = "video/x-matroska";
        public const string VideoAsf = "video/x-ms-asf";

        public static bool IsVideo(string mimeType)
        {
            return new List<string>
            {
                VideoMp4,
                VideoMpeg,
                VideoOgg,
                VideoWebm,
                VideoAvi,
                Video3gpp,
                Video3gpp2,
                VideoMov,
                VideoFlv
            }.Contains(mimeType);
        }

        public static class Text
        {
            public const string TextPlain = "text/plain";
            public const string TextHtml = "text/html";
            public const string TextCss = "text/css";
            public const string TextCsv = "text/csv";
            public const string TextJavascript = "text/javascript";
            public const string TextMarkdown = "text/markdown";
            public const string TextXml = "text/xml";

            public static bool IsText(string mimeType)
            {
                return new List<string>
                {
                    TextPlain,
                    TextHtml,
                    TextCss,
                    TextCsv,
                    TextJavascript,
                    TextMarkdown,
                    TextXml
                }.Contains(mimeType);
            }
        }

        public static class Application
        {
            public const string ApplicationJson = "application/json";
            public const string ApplicationXml = "application/xml";
            public const string ApplicationZip = "application/zip";
            public const string ApplicationGzip = "application/gzip";
            public const string ApplicationPdf = "application/pdf";
            public const string ApplicationMsword = "application/msword";
            public const string ApplicationExcel = "application/vnd.ms-excel";
            public const string ApplicationPowerpoint = "application/vnd.ms-powerpoint";
            public const string ApplicationWoff = "application/font-woff";
            public const string ApplicationWoff2 = "application/font-woff2";
            public const string ApplicationOctetStream = "application/octet-stream";
            public const string ApplicationRtf = "application/rtf";
            public const string ApplicationX7z = "application/x-7z-compressed";
            public const string ApplicationXTar = "application/x-tar";
            public const string ApplicationXGzip = "application/x-gzip";

            public static bool IsApplication(string mimeType)
            {
                return new List<string>
                {
                    ApplicationJson,
                    ApplicationXml,
                    ApplicationZip,
                    ApplicationGzip,
                    ApplicationPdf,
                    ApplicationMsword,
                    ApplicationExcel,
                    ApplicationPowerpoint,
                    ApplicationWoff,
                    ApplicationWoff2,
                    ApplicationOctetStream,
                    ApplicationRtf,
                    ApplicationX7z,
                    ApplicationXTar,
                    ApplicationXGzip
                }.Contains(mimeType);
            }
        }
    }
}
