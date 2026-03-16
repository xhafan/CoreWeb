using System.Collections.ObjectModel;
using CoreDdd.Nhibernate.Configurations;
using CoreDdd.Nhibernate.Repositories;
using CoreDdd.Nhibernate.UnitOfWorks;
using Microsoft.AspNetCore.DataProtection.Repositories;
using System.Xml.Linq;
using MrWatchdog.Core.Infrastructure.DataProtections;

namespace MrWatchdog.Web.Infrastructure;

public class DataProtectionKeyXmlRepository(INhibernateConfigurator nhibernateConfigurator) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        return NhibernateUnitOfWorkRunner.Run<IReadOnlyCollection<XElement>>(
            () => new NhibernateUnitOfWork(nhibernateConfigurator),
            unitOfWork =>
            {
                var dataProtectionKeys = unitOfWork.Session!.QueryOver<DataProtectionKey>().List();
                return new ReadOnlyCollection<XElement>(dataProtectionKeys.Select(x => XElement.Parse(x.Xml)).ToList());
            }
        );
    }

    public void StoreElement(XElement element, string id)
    {
        NhibernateUnitOfWorkRunner.Run(
            () => new NhibernateUnitOfWork(nhibernateConfigurator),
            unitOfWork =>
            {
                var xml = element.ToString();

                var dataProtectionKeyRepository = new NhibernateRepository<DataProtectionKey, string>(unitOfWork);
                var dataProtectionKey = dataProtectionKeyRepository.Get(id);
                if (dataProtectionKey != null)
                {
                    dataProtectionKey.Update(xml);
                }
                else
                {
                    dataProtectionKey = new DataProtectionKey(id, xml);
                    dataProtectionKeyRepository.Save(dataProtectionKey);
                }
            }
        );
    }
}