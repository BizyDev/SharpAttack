using System;
using System.Linq;
using System.Threading;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.Model.Conversation.AudioVideo;

namespace SkypeAPI
{
    public class CallManagement
    {
        private readonly Recorder _audioRecorder;
        private readonly SpeakerRecognitionClient _speakerRecognitionClient;

        private AVModality _avModality;
        private LyncClient _lyncClient;
        private Thread _recordingThread;
        private Conversation _currentConversation;

        public CallManagement()
        {
            _speakerRecognitionClient = new SpeakerRecognitionClient();
            _audioRecorder = new Recorder();
            _audioRecorder.AudioFileCaptured += _audioRecorder_AudioFileCaptured;   
        }

        private void _audioRecorder_AudioFileCaptured(object sender, AudioRecorderEventArgs e)
        {
            var participantIdentifier = _currentConversation.Participants.First(p => !p.IsSelf).Contact.Uri;
            _speakerRecognitionClient.Verify(participantIdentifier, e.AudioFilePath);

        }

        public void Init()
        {
            _lyncClient = LyncClient.GetClient();

            // 
            foreach (var conversation in _lyncClient.ConversationManager.Conversations)
            {
                _avModality = (AVModality)conversation.Modalities[ModalityTypes.AudioVideo];
                _avModality.ModalityStateChanged += AvModality_ModalityStateChanged;
            }

            _lyncClient.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;
            _lyncClient.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;
        }

        private void AvModality_ModalityStateChanged(object sender, ModalityStateChangedEventArgs e)
        {
            Console.WriteLine($"****modality state changed from {e.OldState} to {e.NewState} ****");
            switch (e.NewState)
            {
                case ModalityState.Connected:
                    Console.WriteLine("Call started"); //stop audio recording.

                    var audioProperties = _currentConversation.Modalities[ModalityTypes.AudioVideo].Properties;
                    var device = audioProperties[ModalityProperty.AVModalityAudioRenderDevice];
                    _recordingThread = new Thread(() => { _audioRecorder.Record(device.ToString()); });
                    _recordingThread.Start();

                    break;
                case ModalityState.Disconnected:
                    Console.WriteLine("Call stopped"); //start audio recording.
                    _audioRecorder.Stop();
                    break;
            }
        }

        private void ConversationManager_ConversationRemoved(object sender, ConversationManagerEventArgs e)
        {
            _avModality = (AVModality)e.Conversation.Modalities[ModalityTypes.AudioVideo];
            _avModality.ModalityStateChanged -= AvModality_ModalityStateChanged;
        }

        private void ConversationManager_ConversationAdded(object sender, ConversationManagerEventArgs e)
        {
            _avModality = (AVModality)e.Conversation.Modalities[ModalityTypes.AudioVideo];
            _avModality.ModalityStateChanged += AvModality_ModalityStateChanged;
            _currentConversation = e.Conversation;
        }

    }
}
