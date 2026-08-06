"use client";

import { useState, useRef, useEffect } from "react";
import { MessageCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { motion, AnimatePresence, useDragControls } from "framer-motion";
import AIChatDialog from "@/components/features/ai-assistant/dialogs/AIChatDialog";
import { useUser } from "@clerk/nextjs";
import { getAuthUserRequest } from "@/components/features/users/handlers/getAuthUserRequest";
import { createQueryHookFromTemplate } from "@/hooks/createQueryHookFromTemplate";

const useAuthUser = createQueryHookFromTemplate(getAuthUserRequest);

export function FloatingAIChatButton() {
  const { isLoaded, isSignedIn, user } = useUser();
  const { data: authUser, isLoading: isAuthUserLoading } = useAuthUser({
    pathParams: { id: user?.id ?? "" },
    queryParams: {},
  });

  const canViewChat =
    isLoaded && isSignedIn && !isAuthUserLoading && !!authUser?.data?.role?.key;
  const [position, setPosition] = useState(() => {
    // Safe window access with SSR check
    if (typeof window !== 'undefined') {
      return { x: window.innerWidth - 80, y: window.innerHeight };
    }
    return { x: 0, y: 0 };
  });
  const [isMounted, setIsMounted] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const dragControls = useDragControls();
  const constraintsRef = useRef<HTMLDivElement>(null);

  // Initialize position to bottom-right on mount
  useEffect(() => {
    const updatePosition = () => {
      const padding = 24; // 1.5rem (6 * 4px)
      setPosition({
        x: window.innerWidth - 80 - padding,
        y: window.innerHeight - 80 - padding,
      });
    };

    updatePosition();
    setIsMounted(true);
    window.addEventListener('resize', updatePosition);
    return () => window.removeEventListener('resize', updatePosition);
  }, []);

  const handleClick = () => {
    if (!isDragging) {
      setIsDialogOpen(true);
    }
  };

  // Hide the button for unauthenticated users or users without a role
  if (!canViewChat) return null;

  return (
    <>
      {/* Drag Constraints Container */}
      <div
        ref={constraintsRef}
        className="fixed inset-0 pointer-events-none z-40"
        style={{ overflow: 'hidden' }}
      />

      {/* Floating Button */}
      <AnimatePresence>
        {isMounted && (
          <motion.div
            drag
            dragControls={dragControls}
            dragConstraints={constraintsRef}
            dragElastic={0}
            dragMomentum={false}
            onDragStart={() => setIsDragging(true)}
            onDragEnd={() => {
              // Small delay to prevent click from firing after drag
              setTimeout(() => setIsDragging(false), 100);
            }}
            initial={{
              scale: 0,
              opacity: 0,
              x: typeof window !== 'undefined' ? window.innerWidth - 80 : 0,
              y: typeof window !== 'undefined' ? window.innerHeight : 0
            }}
            animate={{
              scale: 1,
              opacity: 1,
              x: position.x,
              y: position.y,
            }}
            transition={{ duration: 0.3, delay: 2, type: 'spring', stiffness: 300, damping: 20 }}
            className="fixed z-40 cursor-move "
            style={{
              left: 0,
              top: 0,
              pointerEvents: 'auto',
            }}
          >
            <Button
              onClick={handleClick}
              size="icon"
              className="[&_svg]:size-5 h-12 w-12 rounded-full shadow-lg bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 hover:shadow-xl transition-all duration-300 hover:scale-110 cursor-pointer"
            >
              <MessageCircle className="w-8 h-8 text-white" />
            </Button>

            {!isDragging && (
              <div className="absolute bottom-full right-0 mb-2 px-3 py-1.5 bg-gray-900 text-white text-sm rounded-lg whitespace-nowrap opacity-0 hover:opacity-100 transition-opacity pointer-events-none">
                Chat with AI Assistant
                <div className="absolute top-full right-6 w-0 h-0 border-l-4 border-l-transparent border-r-4 border-r-transparent border-t-4 border-t-gray-900" />
              </div>
            )}
          </motion.div>
        )}
      </AnimatePresence>

      {/* AI Chat Dialog */}
      <AIChatDialog open={isDialogOpen} onOpenChange={setIsDialogOpen} />
    </>
  );
}
