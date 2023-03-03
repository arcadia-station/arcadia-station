using Content.Server.Chat.Systems;
using Content.Server.Language;
using Content.Shared.Chat;

namespace Content.Server.Language
{
    enum ChatDataLanguage
    {
        /// <summary>
        /// The language being used for this chat.
        /// </summary>
        Language,

        /// <summary>
        /// The message that entities who do not understand the language receive.
        /// </summary>
        DistortedMessage,
    }

    public sealed class LanguageListener : ChatListenerSystem
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            Before = new Type[] { typeof(SayListenerSystem), typeof(RadioListenerSystem) };

            InitializeListeners();

            _sawmill = Logger.GetSawmill("chat.language");
        }

        public override void OnTransformChat(ref EntityChatTransformEvent args)
        {
            if (!args.Chat.GetData<bool>(ChatDataSay.IsSpoken))
                return;

            // Hook into the old TransformSpeech event for accents.
            args.Chat.Message = _chatSystem.TransformSpeech(args.Chat.Source, args.Chat.Message);

            // Only set Language from LinguisticComponent if it hasn't been set from somewhere else up the chain.
            if (!args.Chat.HasData(ChatDataLanguage.Language) &&
                TryComp<LinguisticComponent>(args.Chat.Source, out var linguisticComponent) &&
                linguisticComponent.ChosenLanguage != null)
            {
                args.Chat.SetData(ChatDataLanguage.Language, linguisticComponent.ChosenLanguage);
            }
        }

        public override void OnRecipientTransformChat(ref GotEntityChatTransformEvent args)
        {
            if (!args.Chat.GetData<bool>(ChatDataSay.IsSpoken))
                return;

            if (!args.Chat.TryGetData<LanguagePrototype>(ChatDataLanguage.Language, out var language))
                // This chat has no specified language, so there's nothing we should
                // do with it.
                //
                // Having null for language is like using a universal translator.
                // Everyone will understand it.
                return;

            /* if (!TryComp<LinguisticComponent>(args.Receiver, out var linguisticComponent)) */
            /* { */
            /*     // The receiver is beyond direct linguistic comprehension */
            /*     return; */
            /* } */

            _sawmill.Debug("here we are");

            if (TryComp<LinguisticComponent>(args.Recipient, out var linguisticComponent) &&
                linguisticComponent.CanUnderstand.Contains(language.ID))
            {
                // The recipient understands us, no mangling needed.
                return;
            }

            _sawmill.Debug("here we are, misunderstood");

            if (language.Distorter == null)
            {
                _sawmill.Error($"Needed to distort a message for language {language.ID} but it has no distorter set.");
                return;
            }

            string distortedMessage;

            if (!args.Chat.TryGetData<string>(ChatDataLanguage.DistortedMessage, out distortedMessage))
            {
                // The distorted version of this message has yet to be
                // generated. It's created only when necessary to save on
                // string manipulation cycles.

                distortedMessage = language.Distorter.Distort(args.Chat.Source, args.Chat.Message);
            }

            args.RecipientData.SetData(ChatRecipientDataSay.Message, distortedMessage);
            _sawmill.Debug("we have been mangled");
        }
    }
}

namespace Content.Server.Chat.Systems
{
    public sealed partial class ChatSystem
    {
        /// <summary>
        /// Try to send a say message from an entity, with a specific language.
        /// </summary>
        public bool TrySendSayWithLanguage(EntityUid source, string message, LanguagePrototype language, EntityUid? speaker = null)
        {
            var chat = new EntityChat(source, message)
            {
                Channel = ChatChannel.Local,
                ClaimedBy = typeof(SayListenerSystem)
            };

            chat.SetData(ChatDataSay.IsSpoken, true);
            chat.SetData(ChatDataLanguage.Language, language);

            if (speaker != null)
                chat.SetData(ChatDataSay.RelayedSpeaker, speaker);

            return TrySendChat(source, chat);
        }
    }
}
