using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace CoreWeb.Infrastructure.Validations;

public class LocalizedValidationMetadataProvider(
    Type resourceType, 
    Func<string, string?> getErrorMessageTranslationForAttributeTypeName
) 
    : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            var attributeType = attribute.GetType().Name;
            var resourceValue = getErrorMessageTranslationForAttributeTypeName(attributeType);
            if (resourceValue == null) continue;

            attribute.ErrorMessageResourceName = attributeType;
            attribute.ErrorMessageResourceType = resourceType;
            attribute.ErrorMessage = null;
        }
    }
}
