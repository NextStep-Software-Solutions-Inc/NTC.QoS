// "use client";

// import React, { useState } from "react";

// import { Button } from "@/components/ui/button";
// import { Input } from "@/components/ui/input";
// import { Label } from "@/components/ui/label";

// const CreateServiceForm = () => {
//   const [serviceType, setServiceType] = useState("");
//   const [serviceName, setServiceName] = useState("");
//   const [serviceNameList, setServiceNameList] = useState<string[]>([]);
//   const [serviceList, setServiceList] = useState<string[]>([]);

//   const handleServiceNameAdd = () => {
//     if (serviceName.trim()) {
//       setServiceNameList((prev) => [...prev, serviceName]);
//       setServiceName("");
//     }
//   };

//   const handleServiceAdd = () => {
//     if (serviceType.trim()) {
//       setServiceList((prev) => [...prev, serviceType]);
//       setServiceType("");
//     }
//   };

//   return (
//     <form className="space-y-4">
//       <section className="flex flex-row gap-4">
//         <div className="flex gap-2 items-end">
//           <div className="flex-1">
//             <Label>Service Types</Label>
//             <Input
//               placeholder="Add Service types"
//               value={serviceType}
//               onChange={(e) => setServiceType(e.target.value)}
//             />
//           </div>
//           <Button type="button" onClick={handleServiceAdd}>
//             Add
//           </Button>
//         </div>

//         <div className="flex gap-2 items-end">
//           <div className="flex-1">
//             <Label>Service Name</Label>
//             <Input placeholder="Add Services" value={serviceName} onChange={(e) => setServiceName(e.target.value)} />
//           </div>
//           <Button type="button" onClick={handleServiceNameAdd}>
//             Add
//           </Button>
//         </div>
//       </section>
//     </form>
//   );
// };

// export default CreateServiceForm;
