using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using MrWatchdog.Core.Resources;
using System.ComponentModel.DataAnnotations;

namespace MrWatchdog.Web.Infrastructure.Validations;

public class LocalizedValidationMetadataProvider : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            var attributeType = attribute.GetType().Name;
            var resourceValue = Resource.ResourceManager.GetString(attributeType);
            if (resourceValue == null) continue;

            attribute.ErrorMessageResourceName = attributeType;
            attribute.ErrorMessageResourceType = typeof(Resource);
            attribute.ErrorMessage = null;
        }
    }
}
