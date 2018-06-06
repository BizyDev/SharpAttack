using System;
using Microsoft.Lync.Model;
using Microsoft.Lync.Model.Conversation;
using Microsoft.Lync.Model.Conversation.AudioVideo;

namespace SkypeAPI
{
    public class CallManagement
    {
        //self participant's AvModality
        private AVModality _avModality;

        //holds the Lync client instance
        private LyncClient _lyncClient;


        private Recorder audioRecorder;

        public void Init()
        {

            audioRecorder = new Recorder();

            _lyncClient = LyncClient.GetClient();
            //if this client is in UISuppressionMode...
            //if (_lyncClient.InSuppressedMode && _lyncClient.State == ClientState.Uninitialized)
            //{
            //    //...need to initialize it
            //    try
            //    {
            //        _lyncClient.BeginInitialize(this.ClientInitialized, null);
            //    }
            //    catch (LyncClientException lyncClientException)
            //    {
            //        Console.WriteLine(lyncClientException);
            //    }
            //}
            //else //not in UI Suppression, so the client was already initialized
            //{
            //    //registers for conversation related events
            //    //these events will occur when new conversations are created (incoming/outgoing) and removed


            //TODO
            foreach (var conversation in _lyncClient.ConversationManager.Conversations)
            {
                _avModality = (AVModality)conversation.Modalities[ModalityTypes.AudioVideo];
                _avModality.ModalityStateChanged += AvModality_ModalityStateChanged;
            }

            _lyncClient.ConversationManager.ConversationAdded += ConversationManager_ConversationAdded;

            _lyncClient.ConversationManager.ConversationRemoved += ConversationManager_ConversationRemoved;


            //    //  
            //}

        }

        private void AvModality_ModalityStateChanged(object sender, ModalityStateChangedEventArgs e)
        {
            Console.WriteLine($"****modality state changed from {e.OldState} to {e.NewState} ****");
            switch (e.NewState)
            {
                case ModalityState.Connected:
                    Console.WriteLine("Call started"); //stop audio recording.
                    audioRecorder.Record();
                    break;
                case ModalityState.Disconnected:
                    Console.WriteLine("Call stopped"); //start audio recording.
                    audioRecorder.Stop();
                    break;
            }
        }

        private void ConversationManager_ConversationRemoved(object sender, ConversationManagerEventArgs e)
        {
            //  throw new NotImplementedException();
         
        }

        private void ConversationManager_ConversationAdded(object sender, ConversationManagerEventArgs e)
        {

            _avModality = (AVModality)e.Conversation.Modalities[ModalityTypes.AudioVideo];
            _avModality.ModalityStateChanged += AvModality_ModalityStateChanged;

        }

    }
}
