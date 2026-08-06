import Image from "next/image";
import Link from "next/link";

interface LogoProps {
  width?: number;
  height?: number;
  href?: string;
  className?: string;
}

const Logo = ({ width = 100, height = 100, href, className }: LogoProps) => {
  const logoElement = (
    <Image src="/assets/images/logo.svg" alt="Logo" width={width} height={height} priority className={className} />
  );

  return href ? <Link href={href}>{logoElement}</Link> : logoElement;
};

export default Logo;
