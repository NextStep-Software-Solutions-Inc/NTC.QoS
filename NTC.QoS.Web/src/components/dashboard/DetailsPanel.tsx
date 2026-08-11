import React from "react";
import Image from "next/image";
import { ArrowDownIcon, ArrowLeft, ArrowUpIcon, CheckCircle2, ClockIcon } from "lucide-react";

interface DetailsPanelProps {
  location: {
    name: string;
    region: string;
    imageUrl: string;
    qualityOfService: { upload: string; download: string; latency: string; availability: string };
    availableProviders: { name: string; color: string; services: string[] }[];
  };
  onClose: () => void;
}

const StatItem: React.FC<{ icon: React.ReactNode; label: string; value: string }> = ({ icon, label, value }) => (
  <div className="flex items-center space-x-3">
    <div className="bg-gray-100 p-2 rounded-lg text-gray-600">{icon}</div>
    <div>
      <p className="text-sm text-gray-500">{label}</p>
      <p className="font-bold text-gray-800">{value}</p>
    </div>
  </div>
);

const DetailsPanel: React.FC<DetailsPanelProps> = ({ location, onClose }) => {
  return (
    <div className="absolute top-0 right-0 h-full w-[360px] bg-white shadow-2xl z-20 flex flex-col animate-slide-in">
      <button
        onClick={onClose}
        className="absolute top-1/2 -left-4 w-8 h-12 bg-gray-800 text-white rounded-l-lg flex items-center justify-center hover:bg-gray-900 focus:outline-none z-30"
        aria-label="Close details panel">
        <ArrowLeft className="w-5 h-5" />
      </button>
      <div className="flex-grow overflow-y-auto">
        <div className="p-3">
          <div className="bg-white rounded-xl shadow-md overflow-hidden">
            <Image className="w-full h-40 object-cover" src={location.imageUrl} alt="Cell tower" />
            <div className="p-4">
              <h2 className="text-2xl font-bold text-gray-900">{location.name}</h2>
              <p className="text-sm text-gray-500">{location.region}</p>
            </div>

            <div className="px-4 pb-4 border-b border-gray-200">
              <h3 className="font-semibold text-gray-700 mb-3">Quality of Service</h3>
              <div className="space-y-3">
                <StatItem
                  icon={<ArrowUpIcon className="w-5 h-5" />}
                  label="Avg. Upload Speed"
                  value={location.qualityOfService.upload}
                />
                <StatItem
                  icon={<ArrowDownIcon className="w-5 h-5" />}
                  label="Avg. Download Speed"
                  value={location.qualityOfService.download}
                />
                <StatItem
                  icon={<ClockIcon className="w-5 h-5" />}
                  label="Avg. Latency"
                  value={location.qualityOfService.latency}
                />
                <StatItem
                  icon={<CheckCircle2 className="w-5 h-5" />}
                  label="Availability"
                  value={location.qualityOfService.availability}
                />
              </div>
            </div>

            <div className="p-4">
              <h3 className="font-semibold text-gray-700 mb-3">Available Providers</h3>
              <div className="space-y-3">
                {location.availableProviders.map((provider) => (
                  <div key={provider.name} className="flex items-start space-x-3">
                    <div
                      className={`w-10 h-10 rounded-lg ${provider.color} flex-shrink-0 flex items-center justify-center text-white font-bold text-lg`}>
                      {provider.name.charAt(0)}
                    </div>
                    <div>
                      <p className="font-semibold text-gray-800">{provider.name}</p>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {provider.services.map((service) => (
                          <span
                            key={service}
                            className="text-xs font-medium bg-gray-200 text-gray-700 px-2 py-0.5 rounded-full">
                            {service}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
      <style>{`
            @keyframes slide-in {
                from { transform: translateX(100%); }
                to { transform: translateX(0); }
            }
            .animate-slide-in {
                animation: slide-in 0.3s ease-out forwards;
            }
        `}</style>
    </div>
  );
};

export default DetailsPanel;
