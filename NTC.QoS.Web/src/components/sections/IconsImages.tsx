import Image from "next/image";

export const QoSLogo = ({ className }: { className: string }) => {
  return <Image alt="QoS logo" width={100} height={100} src={"/assets/svg/logoQA.svg"} className={className} />;
};
