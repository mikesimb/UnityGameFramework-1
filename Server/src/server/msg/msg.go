package msg
import (
	"github.com/name5566/leaf/network/protobuf"
)

var (
	Processor = protobuf.NewProcessor()
)

func init() {	// 这里我们注册了一个 protobuf 消息)
    Processor.Register(&TocNotifyConnect{})
    Processor.Register(&TosChat{})
    Processor.Register(&TocChat{})

}

