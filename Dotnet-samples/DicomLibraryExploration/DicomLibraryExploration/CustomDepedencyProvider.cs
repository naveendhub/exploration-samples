using System;

using Philips.Platform.CommonUtilities.DependencyInjection;
using Philips.Platform.Dicom;

namespace DicomLibraryExploration {
    internal class CustomDepedencyProvider:DependencyProviderBase {
        
        private readonly string dicomTagValidatorInterface = typeof(IPrivateTagValidator).Name;
        /// <summary>
        /// Provide the concrete type that implements <paramref name="requestedInterface"/>.
        /// </summary>
        protected override Type GetConcreteType(Type requestedInterface) {
            var requestedName = requestedInterface.Name;
            // explicitly using fully qualified names to have more expressive code

            if (String.Equals(requestedName, dicomTagValidatorInterface, StringComparison.Ordinal)) {
                return typeof(TagValidator);
            }

            return null;
        }
    }
}
