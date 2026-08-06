import { type Provider } from "../../provider/types/Provider";
import { type Service } from "../../service/types/Service";

export type ServiceProvider = {
  id: string;
  providerId: string;
  provider: Provider;
  serviceId: string;
  service: Service;
  MNC: string;
  MCC: string;
};

export type ServiceProviderDetails = ServiceProvider;
