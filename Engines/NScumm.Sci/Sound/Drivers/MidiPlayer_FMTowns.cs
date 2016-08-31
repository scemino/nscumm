using NScumm.Core.Audio;

namespace NScumm.Sci.Sound.Drivers
{
    internal class MidiPlayer_FMTowns: MidiPlayer
    {
        private readonly MidiDriver_FMTowns _townsDriver;

        public MidiPlayer_FMTowns(SciVersion version) : base(version)
        {
            _driver = _townsDriver = new MidiDriver_FMTowns(SciEngine.Instance.Mixer, version);
        }

        public override bool HasRhythmChannel => false;
        public override byte PlayId => (byte) (_version == SciVersion.V1_EARLY ? 0x00 : 0x16);
        public override int Polyphony => _version == SciVersion.V1_EARLY ? 1 : 6;

        public override MidiDriverError Open(ResourceManager resMan)
        {
            if (_townsDriver == null) return MidiDriverError.DeviceNotAvailable;

            var result = _townsDriver.Open();
            if (result==0 && _version == SciVersion.V1_LATE)
                _townsDriver.LoadInstruments((resMan.FindResource(new ResourceId(ResourceType.Patch, 8), true)).data);
            return result;
        }

        public override void PlaySwitch(bool play)
        {
            _townsDriver?.SetSoundOn(play);
        }
    }
}