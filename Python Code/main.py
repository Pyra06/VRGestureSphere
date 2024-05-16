import cv2
import cvzone
from cvzone.HandTrackingModule import HandDetector
import socket
import math
import numpy as np

# Parameters
height, width = 1280, 720

# Camera Capture
capture_cam = cv2.VideoCapture(0)
capture_cam.set(3, height)
capture_cam.set(4, width)

# Set the window size
cv2.namedWindow("Image", cv2.WINDOW_NORMAL)
default_width = capture_cam.get(cv2.CAP_PROP_FRAME_WIDTH)
default_height = capture_cam.get(cv2.CAP_PROP_FRAME_HEIGHT)
cv2.resizeWindow("Image", int(default_width * 1.1), int(default_height * 1.1))

#Hand Detection
detector = HandDetector(maxHands = 2, detectionCon = 0.8)

# Communication
sckt = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
svrAdrssPrt = ("127.0.0.1", 5052)

while True:
    # Get frames from Camera
    success, img = capture_cam.read()

    # Get the Hands
    hands, img = detector.findHands(img)

    data = []
    right_hand_data = ""
    left_hand_data = ""
    # Landmark Values - (x, y, z) * 21
    if hands:
        for hand in hands:
            lmList = hand['lmList']

            pointA = tuple(lmList[0])
            pointB = tuple(lmList[17])

            # Draw line and circles
            cv2.line(img, (int(pointA[0]), int(pointA[1])), (int(pointB[0]), int(pointB[1])), (255,255,0), 3)  # Only using x and y for line
            cv2.circle(img, (int(pointA[0]), int(pointA[1])), 5, (255,0,255), cv2.FILLED)  # Only using x and y for circle
            cv2.circle(img, (int(pointB[0]), int(pointB[1])), 5, (255,0,255), cv2.FILLED)  # Only using x and y for circle

            # Calculate distances
            w1 = np.sqrt((pointB[2])**2 + (np.sqrt((pointB[0] - pointA[0])**2 + (pointB[1] - pointA[1])**2))**2)
            w2 = np.sqrt((pointB[0] - pointA[0])**2 + (pointB[1] - pointA[1])**2)
            #print(w2, w1, pointB[2], (pointB[2])**2, "+", w2**2, "=", (pointB[2])**2 + w2**2)
            W = 8

            # #Finding Focal Length
            # distance = 30
            # f = (w * distance) / W
            # print(f)

            #Finding Depth in cm
            f = 400
            distance = (W * f)/ w1

            hand_data = []
            for lm in lmList:
                x = lm[0]
                y = (height - lm[1])
                z = (distance * 15 + lm[2])
                hand_data.extend([x, y, z])
            data.append(hand_data)
            
            if hand['type'] == 'Right':
                right_hand_data = str(data[hands.index(hand)])
            elif hand['type'] == 'Left':
                left_hand_data = str(data[hands.index(hand)])

    #sckt.sendto(str.encode(f"R {right_hand_data} L {left_hand_data}"), svrAdrssPrt)
    sckt.sendto(str.encode(f"L {left_hand_data} R {right_hand_data}"), svrAdrssPrt)

    cv2.imshow("Image", img)
    if cv2.waitKey(1) == ord('q'):  # Press 'q' to quit
        break

cv2.destroyAllWindows()
capture_cam.release()