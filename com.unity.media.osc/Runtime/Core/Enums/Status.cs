namespace Unity.Media.Osc
{
    /// <summary>
    /// The operational statuses for <see cref="OscClient"/> and <see cref="OscServer"/>.
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// The <see cref="OscClient"/> or <see cref="OscServer"/> is inactive.
        /// </summary>
        None = 0,
        /// <summary>
        /// The <see cref="OscClient"/> or <see cref="OscServer"/> is operating normally.
        /// </summary>
        Ok = 1,
        /// <summary>
        /// The <see cref="OscClient"/> or <see cref="OscServer"/> is encountering issues, but is still partially operational.
        /// </summary>
        Warning = 2,
        /// <summary>
        /// The <see cref="OscClient"/> or <see cref="OscServer"/> is unable to function as configured.
        /// </summary>
        Error = 3,
    }
}
