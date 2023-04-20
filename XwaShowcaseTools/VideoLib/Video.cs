using MediaFoundation;
using MediaFoundation.ReadWrite;
using System;
using System.Runtime.InteropServices;

namespace VideoLib
{
    public sealed class Video
    {
        private static void Startup()
        {
            Marshal.ThrowExceptionForHR((int)MFExtern.MFStartup(0x20070, MFStartup.Full));
        }

        private static void Shutdown()
        {
            Marshal.ThrowExceptionForHR((int)MFExtern.MFShutdown());
        }

        private static void InitializeSinkWriter(string outputUrl, int width, int height, int fps, int bitrate, out IMFSinkWriter writer, out int videoStreamIndex)
        {
            Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateAttributes(out IMFAttributes attributes, 0));

            try
            {
                Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1));
                Marshal.ThrowExceptionForHR((int)attributes.SetUINT32(MFAttributesClsid.MF_SINK_WRITER_DISABLE_THROTTLING, 1));

                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateSinkWriterFromURL(outputUrl, null, attributes, out writer));
            }
            finally
            {
                Marshal.ReleaseComObject(attributes);
            }

            try
            {
                // Set the video output media type.
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out IMFMediaType videoMediaTypeOut));

                try
                {
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.H264));

                    if (bitrate > 0)
                    {
                        //Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AVG_BITRATE, 2000000));
                        Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_AVG_BITRATE, bitrate));
                    }

                    Marshal.ThrowExceptionForHR((int)videoMediaTypeOut.SetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, (int)MFVideoInterlaceMode.Progressive));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeSize(videoMediaTypeOut, MFAttributesClsid.MF_MT_FRAME_SIZE, width, height));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeOut, MFAttributesClsid.MF_MT_FRAME_RATE, fps, 1));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeOut, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO, 1, 1));
                    Marshal.ThrowExceptionForHR((int)writer.AddStream(videoMediaTypeOut, out videoStreamIndex));
                }
                finally
                {
                    Marshal.ReleaseComObject(videoMediaTypeOut);
                }

                // Set the video input media type.
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMediaType(out IMFMediaType videoMediaTypeIn));

                try
                {
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.RGB32));
                    Marshal.ThrowExceptionForHR((int)videoMediaTypeIn.SetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, (int)MFVideoInterlaceMode.Progressive));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeSize(videoMediaTypeIn, MFAttributesClsid.MF_MT_FRAME_SIZE, width, height));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeIn, MFAttributesClsid.MF_MT_FRAME_RATE, fps, 1));
                    Marshal.ThrowExceptionForHR((int)MFExtern.MFSetAttributeRatio(videoMediaTypeIn, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO, 1, 1));
                    Marshal.ThrowExceptionForHR((int)writer.SetInputMediaType(videoStreamIndex, videoMediaTypeIn, null));
                }
                finally
                {
                    Marshal.ReleaseComObject(videoMediaTypeIn);
                }
            }
            catch
            {
                Marshal.ReleaseComObject(writer);
                throw;
            }

            // Tell the sink writer to start accepting data.
            Marshal.ThrowExceptionForHR((int)writer.BeginWriting());
        }

        private static void WriteVideoFrame(int width, int height, long frameDuration, IMFSinkWriter writer, int videoStreamIndex, long rtStart, byte[] videoFrameBuffer)
        {
            Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateSample(out IMFSample sample));

            try
            {
                Marshal.ThrowExceptionForHR((int)MFExtern.MFCreateMemoryBuffer(videoFrameBuffer.Length, out IMFMediaBuffer buffer));

                try
                {
                    Marshal.ThrowExceptionForHR((int)buffer.Lock(out IntPtr pData, out int maxLength, out int currentLength));

                    try
                    {
                        //Marshal.Copy(videoFrameBuffer, 0, pData, videoFrameBuffer.Length);

                        for (int y = 0; y < height; y++)
                        {
                            Marshal.Copy(videoFrameBuffer, (height - 1 - y) * width * 4, pData + y * width * 4, width * 4);
                        }
                    }
                    finally
                    {
                        Marshal.ThrowExceptionForHR((int)buffer.Unlock());
                    }

                    Marshal.ThrowExceptionForHR((int)buffer.SetCurrentLength(videoFrameBuffer.Length));
                    Marshal.ThrowExceptionForHR((int)sample.AddBuffer(buffer));
                }
                finally
                {
                    Marshal.ReleaseComObject(buffer);
                }

                Marshal.ThrowExceptionForHR((int)sample.SetSampleTime(rtStart));
                Marshal.ThrowExceptionForHR((int)sample.SetSampleDuration(frameDuration));
                Marshal.ThrowExceptionForHR((int)writer.WriteSample(videoStreamIndex, sample));
            }
            finally
            {
                Marshal.ReleaseComObject(sample);
            }
        }

        private IMFSinkWriter _writer;
        private int _videoStream;

        public string FileName { get; private set; }

        public int Fps { get; private set; }

        public long FrameDuration { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public long CurrentTime { get; private set; }

        public static Video Open(string fileName, int fps, int width, int height, int bitrate)
        {
            var video = new Video
            {
                FileName = fileName,
                Fps = fps,
                FrameDuration = 10 * 1000 * 1000 / fps,
                Width = width,
                Height = height,
                CurrentTime = 0
            };

            Startup();

            try
            {
                InitializeSinkWriter(
                    fileName,
                    width,
                    height,
                    fps,
                    bitrate,
                    out video._writer,
                    out video._videoStream);
            }
            catch
            {
                Shutdown();
                throw;
            }

            return video;
        }

        public void Close()
        {
            _writer.Finalize_();
            Marshal.ReleaseComObject(_writer);
            Shutdown();
        }

        public void AppendFrame(byte[] videoData)
        {
            if (videoData is null)
            {
                throw new ArgumentNullException(nameof(videoData));
            }

            if (videoData.Length != Width * Height * 4)
            {
                throw new ArgumentOutOfRangeException(nameof(videoData));
            }

            WriteVideoFrame(Width, Height, FrameDuration, _writer, _videoStream, CurrentTime, videoData);
            CurrentTime += FrameDuration;
        }
    }
}
